using System.IO;
using Unity.MLAgents;
using UnityEngine;

namespace RealmAI {
	public class RealmAgent : Agent {
		[SerializeField] private RealmSensorComponent _realmSensor = default;
		[SerializeField] private RealmScore _realmScore = default;
		[SerializeField] private RealmOwl _realmOwl = default;
		[SerializeField] private RealmRecorder _realmRecorder = default;
		[SerializeField] private SerializedAction _initializeFunction = default;
		[SerializeField] private SerializedAction _resetFunction = default;
		[SerializeField] private SerializedBoolFunc _gameOverFunction = default;
		[SerializeField] private float _episodeTimeout = -1;

		public bool IsActive => isActiveAndEnabled;

		private string _saveDirectory = "";

		public string SaveDirectory {
			get {
				if (string.IsNullOrEmpty(_saveDirectory)) {
					_saveDirectory = GetSaveDirectory();
				}

				return _saveDirectory;
			}
		}

		private float _lastReward = 0;
		private float _episodeDuration = 0;
		private bool _initialized = false;


		public override void OnEpisodeBegin() {
			_lastReward = 0;
			_episodeDuration = 0;
			_realmSensor.StartNewEpisode();
			_realmScore.StartNewEpisode();
			_realmOwl.StartNewEpisode(CompletedEpisodes);
			_realmRecorder.StartEpisode(CompletedEpisodes, Academy.Instance.TotalStepCount);
			if (!_initialized) {
				_initialized = true;
				_initializeFunction?.Invoke();
			}
			_resetFunction?.Invoke();
		}

		private void Update() {
			UpdateReward();
			_episodeDuration += Time.deltaTime;
			_realmOwl.RecordDuration(_episodeDuration);

			if (_gameOverFunction?.Invoke() ?? false) {
				EndEpisode();
			} else if (_episodeTimeout > 0 && _episodeDuration > _episodeTimeout) {
				EndEpisode();
			}
		}

		private void UpdateReward() {
			var currentReward = _realmScore.GetScore();
			var diff = currentReward - _lastReward;
			if (Mathf.Abs(diff) > 1e-7) {
				AddReward(diff);
				_lastReward = currentReward;
				_realmOwl.RecordReward(currentReward);
				_realmRecorder.RecordReward(currentReward);
			}
		}


		private string GetSaveDirectory() {
			if (!Application.isEditor) {
				// get directory from args
				var args = System.Environment.GetCommandLineArgs();

				// parse from command line arguments
				var parseLogFile = false;
				var logFileArg = "";
				var parseDataPath = false;
				var customPathArg = "";
				foreach (var arg in args) {
					if (parseLogFile && logFileArg == "") {
						logFileArg = arg;
						parseLogFile = false;
					} else if (parseDataPath && customPathArg == "") {
						customPathArg = arg;
						parseDataPath = false;
					} else {
						switch (arg) {
							case "-logFile":
								parseLogFile = true;
								break;
							case "-realmData":
								parseDataPath = true;
								break;
						}
					}
				}

				if (!string.IsNullOrEmpty(customPathArg)) {
					// custom path is provided
					return customPathArg;
				}

				if (!string.IsNullOrEmpty(logFileArg)) {
					// no custom path provided, try save to the same directory as ML-Agents 
					var directory = Path.GetDirectoryName(logFileArg);
					if (directory != null && directory.EndsWith("run_logs")) {
						return Path.GetFullPath(Path.Combine(directory, "..", "RealmAI"));
					}
				}
			}
			
#if UNITY_EDITOR
			// when training in the editor, the training runner should have identified a directory for us:
			var settings = RealmEditorSettings.LoadUserSettings();
			if (!string.IsNullOrEmpty(settings.CurrentResultsDirectory)) {
				return Path.Combine(settings.CurrentResultsDirectory, "RealmAI");
			}
#endif

			return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "RealmAI", "Results", "misc");
		}
	}
}