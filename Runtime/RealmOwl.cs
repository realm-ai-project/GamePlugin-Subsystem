using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RealmAI {
	public class RealmOwl : MonoBehaviour {
		public enum StorageFormat {
			Compact,
			Json
		}

		[SerializeField] private string _filePrefix = "data";
		[SerializeField] private StorageFormat _storageFormat = StorageFormat.Compact;
		[SerializeField] private float _flushPeriod = 60;

		// For each instance of this class, assign an integer Id.
		// This is used to create a unique file name for writing to. 
		private static int InstancesCount = 0;
		private int _instanceId = 99999;	
		
		private DataFileWriter _fileWriter = null;
		private bool _firstEpisode = true;
		private int _episodeNum = -1;
		private float _duration = 0;
		private float _reward = 0;
		private List<Vector2> _positions = new List<Vector2>();

		private void Awake() {
			_instanceId = InstancesCount;
			InstancesCount++;
		}
		
		private void Start() {
			StartCoroutine(Flusher());
		}

		private void InitializeFile() {
			if (_fileWriter != null) {
				Debug.LogWarning("Data storage file is being initialized multiple times!");
				return;
			}
			
			// get file extension and format
			string fileExtension = "";
			switch (_storageFormat) {
				case StorageFormat.Compact:
					fileExtension = "dat";
					_fileWriter = new BinaryDataFileWriter();
					break;
				case StorageFormat.Json:
					fileExtension = "json";
					_fileWriter = new JsonDataFileWriter();
					break;
				default:
					Debug.LogError($"Storage format {_storageFormat} is not supported.");
					return;
			}
			
			// get directory
			var saveDirectory = "";
			if (!Application.isEditor) {
				var args = System.Environment.GetCommandLineArgs();

				// parse from command line arguments
				var parseLogFile = false;
				var logFileArg = "";
				var parseCustomPath = false;
				var customPathArg = "";
				foreach (var arg in args) {
					if (parseLogFile && logFileArg == "") {
						logFileArg = arg;
						parseLogFile = false;
					} else if (parseCustomPath && customPathArg == "") {
						customPathArg = arg;
						parseCustomPath = false;
					} else {
						switch (arg) {
							case "-logFile":
								parseLogFile = true;
								break;
							case "-realmData":
								parseCustomPath = true;
								break;
						}
					}

					Debug.Log(arg);
				}

				if (!string.IsNullOrEmpty(customPathArg)) {
					// custom path is provided
					saveDirectory = customPathArg;
				} else if (!string.IsNullOrEmpty(logFileArg)) {
					// no custom path provided, try save to the same directory as ML-Agents 
					var directory = Path.GetDirectoryName(logFileArg);
					if (directory != null && directory.EndsWith("run_logs")) {
						saveDirectory = $"{directory}/../RealmAI";
					}
				}
			}
			
			// TODO see if we want to save data elsewhere when training in editor...
			if (string.IsNullOrEmpty(saveDirectory)) {
				saveDirectory = $"{Application.dataPath}/RealmAI/Data";
			}
			
			// TODO: validate path and handle errors
			// TODO: consider policy for overwriting
			// TODO: handle relative path?
			
			// open file for writing
			Directory.CreateDirectory(saveDirectory);
			var count = _instanceId;
			while (count < 1e5) {
				try {
					var path = $"{saveDirectory}/{_filePrefix}-{count}.{fileExtension}";
					var fileExists = File.Exists(path);
					var fileStream = File.Open(path, FileMode.Create, FileAccess.Write);
					_fileWriter.Initialize(fileStream);
					if (fileExists) {
						Debug.Log($"Overwriting existing data on {path}");
					} else {
						Debug.Log($"Saving data to {path}");
					}
					break;
				} catch (IOException e) {
					// exception for file sharing violation
					if (e.HResult != -2147024864) {
						Debug.LogException(e);
						break;
					}
				}

				count++;
			}
		}

		public void StartNewEpisode(int episodeNum) {
			if (_firstEpisode) {
				_firstEpisode = false;
				InitializeFile();
			} else {
				WriteEpisode();
			}

			_episodeNum = episodeNum;
			_duration = 0;
			_reward = 0;
			_positions.Clear();
		}

		public void RecordDuration(float duration) {
			_duration = duration;
		}

		public void RecordReward(float reward) {
			_reward = reward;
		}

		public void RecordPosition(Vector2 position) {
			_positions.Add(position);
		}

		private void WriteEpisode() {
			if (_fileWriter == null) {
				Debug.LogError("File uninitialized");
				return;
			}

			_fileWriter.WriteEpisode(_episodeNum, _duration, _reward, _positions);
		}

		private void Close() {
			if (_fileWriter != null) {
				WriteEpisode();
			}

			_fileWriter?.Close();
			_fileWriter = null;
		}

		private void OnDestroy() {
			Close();
		}

		private void OnApplicationQuit() {
			Close();
		}

		private IEnumerator Flusher() {
			while (true) {
				yield return new WaitForSeconds(_flushPeriod);
				_fileWriter?.Flush();
			}
		}
	}
}
