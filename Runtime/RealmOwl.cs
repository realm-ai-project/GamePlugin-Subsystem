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
		private DataFileWriter _fileWriter = null;

		private bool _isAlive = true;
		private bool _firstEpisode = true;
		private int _episodeNum = -1;
		private float _duration = 0;
		private float _reward = 0;
		private List<Vector2> _positions = new List<Vector2>();

		private void Start() {
			StartCoroutine(Flusher());
		}

		private void InitializeFile() {
			if (_fileWriter != null) {
				Debug.LogWarning("Data storage file is being initialized multiple times!");
				return;
			}
			
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
			
			// TODO: make multiple processsing saving to a file more robust
			// TODO: consider policy for overwriting
			// TODO: consider if we want separate threads for writing
			var count = 0;
			while (count < 256) {
				try {
					var path = $"{_filePrefix}-{count}.{fileExtension}";
					var fileStream = File.OpenWrite(path);
					_fileWriter.Initialize(fileStream);
					Debug.Log($"Data saving to {path}");
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

		public void WriteEpisode() {
			if (!_isAlive) {
				return;
			}

			if (_fileWriter == null) {
				InitializeFile();
				if (_fileWriter == null) {
					return;
				}
			}

			_fileWriter.WriteEpisode(_episodeNum, _duration, _reward, _positions);
		}

		private void OnDestroy() {
			WriteEpisode();

			_isAlive = false;
			_fileWriter?.Close();
		}

		private void OnApplicationQuit() {
			WriteEpisode();

			_isAlive = false;
			_fileWriter?.Close();
		}

		private IEnumerator Flusher() {
			while (true) {
				yield return new WaitForSeconds(_flushPeriod);
				_fileWriter?.Flush();
			}
		}
	}
}
