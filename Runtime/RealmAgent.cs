using System;
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
        [SerializeField] private BoolDelegate _gameoverFunction = default;
        
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
            
            if (_gameoverFunction != null) {
                if (_gameoverFunction.Invoke()) {
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
	        var saveDirectory = "";
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
		        if (Application.isEditor) {
			        saveDirectory = $"{Path.GetDirectoryName(Application.dataPath)}/RealmAI/Results/Editor";
		        } else {
			        saveDirectory = $"{Application.dataPath}/RealmAI/Results/Build";
		        }
	        }

	        return saveDirectory;
        }
    }
}