using System.IO;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

namespace RealmAI {
    public class RealmAgent : Agent {
        [SerializeField] private RealmOwl _realmOwl = default;
        [SerializeField] private RealmRecorder _realmRecorder = default;
        [SerializeField] private UnityEvent _episodeReset = default;
        [SerializeField] private FloatDelegate _rewardFunction = default;
        [SerializeField] private BoolDelegate _gameOverFunction = default;
        
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


        public override void OnEpisodeBegin() {
            _lastReward = 0;
            _episodeDuration = 0;
            _realmOwl.StartNewEpisode(CompletedEpisodes);
            _realmRecorder.StartEpisode(CompletedEpisodes, Academy.Instance.TotalStepCount);
            _episodeReset.Invoke();
        }

        private void Update() {
            UpdateReward();
            _episodeDuration += Time.deltaTime;
            _realmOwl.RecordDuration(_episodeDuration);
            
            if (_gameOverFunction != null) {
                if (_gameOverFunction.Invoke()) {
                    EndEpisode();
                }
            }
        }

        private void UpdateReward() {
            var currentReward = _rewardFunction?.Invoke() ?? 0;
            var diff = currentReward - _lastReward;
            if (Mathf.Abs(diff) > 1e-7) {
                AddReward(diff);
                _lastReward = currentReward;
                _realmOwl.RecordReward(currentReward);
                _realmRecorder.RecordReward(currentReward);
            }
        }

        private string GetSaveDirectory() {
	        // get directory
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
			        return customPathArg;
		        }
		        
		        if (!string.IsNullOrEmpty(logFileArg)) {
			        // no custom path provided, try save to the same directory as ML-Agents 
			        var directory = Path.GetDirectoryName(logFileArg);
			        if (directory != null && directory.EndsWith("run_logs")) {
				        return $"{directory}/../RealmAI";
			        }
		        }
	        }
	        
#if UNITY_EDITOR
	        
			// when training in the editor, the the training runner should have identified a directory for us:
			// TODO very temp solution for getting save folder from python gui
			var hackyTempFilePath = $"{Path.GetDirectoryName(Application.dataPath)}/RealmAI/Python-GUI/last_run.txt";
			if (File.Exists(hackyTempFilePath)) {
				return File.ReadAllText(hackyTempFilePath);
			}
#endif
	        return $"{Path.GetDirectoryName(Application.dataPath)}/RealmAI/Results/default";
        }
    }
}