using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    public class TrainingRunner : MonoBehaviour {
        private static string TrainingUtilsDir => $"{Path.GetDirectoryName(Application.dataPath)}/RealmAI/Training";
        private static string BaseResultsDir => $"{Path.GetDirectoryName(Application.dataPath)}/RealmAI/Results";
        private const string TemplatesPath = "Packages/com.realmai.unity/Editor/Templates";
        
        private const string EditorTrainingScript = "train-editor.bat";
        private const string BuildTrainingScript = "train-build.bat";
        private const string EditorTrainingConfig = "train-editor-config.yaml";
        private const string BuildTrainingConfig = "train-build-config.yaml";

        private const string DefaultTrainingBuildName = "TrainingBuild";

        private const string DateTimeFormat = "yyyy-mm-dd_hh-mm-ss";
        
        // TODO create training runner files on initialize so they can be edited by user
        // TODO add for mac and linux
        // TODO allow configuration for: output folder, run id, custom config file, etc.?
        [MenuItem("Realm AI/Train in Editor")]
        private static void TrainInEditor() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                // create results directory
                var behaviorName = FindBehaviorName();
                var timeStr = DateTime.Now.ToString(DateTimeFormat);
                var resultsDir = $"{BaseResultsDir}/Editor-{behaviorName}-{timeStr}";
                Directory.CreateDirectory(resultsDir);

                // run scripts
                EnsureTrainingScriptsExist();
                var scriptPath = $"{TrainingUtilsDir}/{EditorTrainingScript}";
                var configPath = $"{TrainingUtilsDir}/{EditorTrainingConfig}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/K \"\"{scriptPath}\" \"{configPath}\" \"{resultsDir}\"\"";
                Process.Start(startInfo);
                
                WaitForTrainerAndPlay();
            } else {
                Debug.Log("Train in Editor: Stop playing in the editor and try again.");
            }
        }
        
         
        private static async void WaitForTrainerAndPlay ()
        {
            // TODO ideally this would wait until trainer is ready before playing, not just a set duration.
            await Task.Delay(3000);
            EditorApplication.EnterPlaymode();
            Debug.Log("Started processes for training in editor.");
        }
        
        
        [MenuItem("Realm AI/Build and Train")]
        private static void TrainWithBuild() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                var buildPath = EditorUtility.SaveFilePanel("Choose Location of Built Game", "", DefaultTrainingBuildName, "exe");
                if (string.IsNullOrEmpty(buildPath))
                    return;

                var behaviorName = FindBehaviorName();
                
                Debug.Log("Build started...");
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows, BuildOptions.None);

                Debug.Log("Build for training completed, starting training process...");

                // create results directory
                var timeStr = DateTime.Now.ToString(DateTimeFormat);
                var resultsDir = $"{BaseResultsDir}/Build-{behaviorName}-{timeStr}";
                Directory.CreateDirectory(resultsDir);

                // run scripts
                EnsureTrainingScriptsExist();
                var scriptPath = $"{TrainingUtilsDir}/{BuildTrainingScript}";
                var configPath = $"{TrainingUtilsDir}/{BuildTrainingConfig}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/K \"\"{scriptPath}\" \"{configPath}\" \"{buildPath}\" \"{behaviorName}\" \"{resultsDir}\"\"";

                Process.Start(startInfo);
            } else {
                Debug.Log("Build and Train: Stop playing in the editor and try again.");
            }
        }

        private static void EnsureTrainingScriptsExist() {
            Directory.CreateDirectory(TrainingUtilsDir);
            if (!File.Exists($"{TrainingUtilsDir}/{EditorTrainingScript}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{EditorTrainingScript}", $"{TrainingUtilsDir}/{EditorTrainingScript}");
                Debug.Log($"Editor training script has been created at: {TrainingUtilsDir}/{EditorTrainingScript}");
            }

            if (!File.Exists($"{TrainingUtilsDir}/{EditorTrainingConfig}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{EditorTrainingConfig}", $"{TrainingUtilsDir}/{EditorTrainingConfig}");
                Debug.Log($"Editor training config has been created at: {TrainingUtilsDir}/{EditorTrainingConfig}");
            }

            if (!File.Exists($"{TrainingUtilsDir}/{BuildTrainingScript}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{BuildTrainingScript}", $"{TrainingUtilsDir}/{BuildTrainingScript}");
                Debug.Log($"Build training script has been created at: {TrainingUtilsDir}/{BuildTrainingScript}");
            }

            if (!File.Exists($"{TrainingUtilsDir}/{BuildTrainingConfig}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{BuildTrainingConfig}", $"{TrainingUtilsDir}/{BuildTrainingConfig}");
                Debug.Log($"Build training config has been created at: {TrainingUtilsDir}/{BuildTrainingConfig}");
            }
        }

        private static string FindBehaviorName() {
            // TODO we can probably get this from somewhere else
            foreach (var sceneRootObject in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach (var behaviorParameter in sceneRootObject.GetComponentsInChildren<BehaviorParameters>()) {
                    if (!string.IsNullOrEmpty(behaviorParameter.BehaviorName)) {
                        return behaviorParameter.BehaviorName;
                    }
                }
            }

            return "Player";
        }
    }
}
