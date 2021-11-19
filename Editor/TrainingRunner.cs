using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        
        private const string EnvSetupScript = "env-setup.bat";
        private const string EditorTrainingScript = "train-editor.bat";
        private const string BuildTrainingScript = "train-build.bat";
        private const string EditorTrainingConfig = "train-editor-config.yaml";
        private const string BuildTrainingConfig = "train-build-config.yaml";

        private const string DefaultTrainingBuildName = "TrainingBuild";

        private const string DateTimeFormat = "yyyy-MM-dd_hh-mm-ss";
        
        private static StringBuilder _sb = new StringBuilder();
        // TODO add for mac and linux
        [MenuItem("Realm AI/Train in Editor")]
        private static void TrainInEditor() {
            // TODO hide command line window
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                var behaviorName = GetBehaviorName() ?? FindBehaviorNameFromScene();

                // run scripts
                EnsureTrainingScriptsExist();
                var envSetupPath = $"{TrainingUtilsDir}/{EnvSetupScript}";
                var scriptPath = $"{TrainingUtilsDir}/{EditorTrainingScript}";

                // run training process
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/K \"\"{envSetupPath}\" && \"{scriptPath}\" \"{behaviorName}\"\"";
                using (var process = Process.Start(startInfo)) {
                    if (process == null) {
                        Debug.LogError("Failed to start training process");
                        return;
                    }
                }

                RealmEditorTrainingWaitWindow.ShowWindow();
            } else {
                Debug.Log("Train in Editor: Stop playing in the editor and try again.");
            }
        }

        [MenuItem("Realm AI/Build and Train")]
        private static void TrainWithBuild() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                var buildPath = EditorUtility.SaveFilePanel("Choose Location of Built Game", "", DefaultTrainingBuildName, "exe");
                if (string.IsNullOrEmpty(buildPath))
                    return;

                var behaviorName = GetBehaviorName() ?? FindBehaviorNameFromScene();
                
                Debug.Log("Build started...");
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows, BuildOptions.None);

                Debug.Log("Build for training completed, starting training process...");

                // run scripts
                EnsureTrainingScriptsExist();
                var envSetupPath = $"{TrainingUtilsDir}/{EnvSetupScript}";
                var scriptPath = $"{TrainingUtilsDir}/{BuildTrainingScript}";
                var configPath = $"{TrainingUtilsDir}/{BuildTrainingConfig}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/K \"\"{envSetupPath}\" && \"{scriptPath}\" \"{behaviorName}\" \"{buildPath}\"\"";

                Process.Start(startInfo);
            } else {
                Debug.Log("Build and Train: Stop playing in the editor and try again.");
            }
        }

        private static void EnsureTrainingScriptsExist() {
            Directory.CreateDirectory(TrainingUtilsDir);
            if (!File.Exists($"{TrainingUtilsDir}/{EnvSetupScript}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{EnvSetupScript}", $"{TrainingUtilsDir}/{EnvSetupScript}");
                Debug.Log($"Environment setup script has been created at: {TrainingUtilsDir}/{EnvSetupScript}");
            }
            
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

        private static string GetBehaviorName() {
            var settings = RealmEditorSettings.LoadSettings();
            if (settings == null || string.IsNullOrEmpty(settings.PlayerPrefabGuid)) {
                Debug.LogWarning("The player prefab has not been configured. Set it up in the Realm AI configuration window first!");
                return null;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(settings.PlayerPrefabGuid));
            if (prefab == null) {
                Debug.LogWarning("The player prefab configuration seems to be broken.");
                return null;
            }

            var behaviorParameters = prefab.GetComponentInChildren<BehaviorParameters>();
            if (behaviorParameters == null) {
                Debug.LogWarning("The player prefab has not been set up with the Realm AI Module. Use the Realm AI configuration window to set this up.");
                return null;
            }

            return behaviorParameters.BehaviorName;
        }

        private static string FindBehaviorNameFromScene() {
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
