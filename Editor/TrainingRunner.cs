using System;
using System.Diagnostics;
using System.IO;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    [InitializeOnLoadAttribute]
    public class TrainingRunner : MonoBehaviour {
        private static string FilesInProjectDir => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "RealmAI");
        private static string TrainingUtilsDir => Path.Combine(FilesInProjectDir, "Training");
        private static string BaseResultsDir =>  Path.Combine(FilesInProjectDir, "Results");
        private static string DashboardPath => Path.Combine(FilesInProjectDir, "Dashboard", "index.html");
        private static string DashboardApiDir => Path.Combine(FilesInProjectDir, "Dashboard", "api");
        private static string TemplatesPath => Path.Combine("Packages", "com.realmai.unity", "Editor", "Templates");

#if UNITY_EDITOR_WIN
        private const string ScriptExt = "bat";
        private const string ScriptProcess = "cmd";
        private const string ScriptProcessArgsPreamble = "/K";
#else
        private const string ScriptExt = "sh";
        private const string ScriptProcess = "sh";
        private const string ScriptProcessArgsPreamble = "-c";
#endif
        
        private static string EnvSetupScript => $"env-setup.{ScriptExt}";
        private static string EditorTrainingScript => $"train-editor.{ScriptExt}";
        private static string BuildTrainingScript => $"train-build.{ScriptExt}";
        private static string DashboardApiScript => $"dashboard-api.{ScriptExt}";
        
        private const string DefaultTrainingBuildName = "TrainingBuild";
        private const string DateTimeFormat = "yyyy-MM-dd_hh-mm-ss";

        static TrainingRunner(){
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                SaveCurrentResultsDirectory("");
            }
            
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Realm AI/Train in Editor")]
        private static void TrainInEditor() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                var behaviorName = GetBehaviorName() ?? FindBehaviorNameFromScene();

                // run scripts
                EnsureTemplatesExist();
                var envSetupPath = Path.Combine(TrainingUtilsDir, EnvSetupScript);
                var scriptPath = Path.Combine(TrainingUtilsDir, EditorTrainingScript);
                var runId = $"{behaviorName}_{DateTime.Now.ToString(DateTimeFormat)}";
                var resultsDir = Path.Combine(BaseResultsDir, "Editor");

                // TODO: feels weird? saving this path so we can access it when training in editor
                SaveCurrentResultsDirectory(Path.Combine(resultsDir, runId));
                
                // var startInfo = new ProcessStartInfo(ScriptProcess);
                // startInfo.WindowStyle = ProcessWindowStyle.Normal;
                // startInfo.Arguments = $"/K \"\"{envSetupPath}\" && \"{scriptPath}\" \"{resultsDir}\" {runId}\"";
                var startInfo = new ProcessStartInfo(ScriptProcess);
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"{ScriptProcessArgsPreamble} \"\"{envSetupPath}\" && \"{scriptPath}\" \"{resultsDir}\" {runId}\"";
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
                EnsureTemplatesExist();
                var envSetupPath = Path.Combine(TrainingUtilsDir, EnvSetupScript);
                var scriptPath = Path.Combine(TrainingUtilsDir, BuildTrainingScript);
                var runId = $"{behaviorName}_{DateTime.Now.ToString(DateTimeFormat)}";
                var resultsDir = Path.Combine(BaseResultsDir, "Build");

                // TODO: don't need this for build
                SaveCurrentResultsDirectory(Path.Combine(resultsDir, runId));

                var startInfo = new ProcessStartInfo(ScriptProcess);
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"{ScriptProcessArgsPreamble} \"\"{envSetupPath}\" && \"{scriptPath}\" \"{resultsDir}\" {runId} \"{buildPath}\"\"";

                Process.Start(startInfo);
            } else {
                Debug.Log("Build and Train: Stop playing in the editor and try again.");
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

        private static void SaveCurrentResultsDirectory(string directory) {
            var settings = RealmEditorSettings.LoadSettings();
            settings.CurrentResultsDirectory = directory;
            RealmEditorSettings.SaveSettings(settings);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                SaveCurrentResultsDirectory("");
            }
        }
        
        [MenuItem("Realm AI/Open Results Directory")]
        private static void OpenResultsDirectory() {
            Directory.CreateDirectory(BaseResultsDir);
            var startInfo = new ProcessStartInfo(BaseResultsDir);
            using (var process = Process.Start(startInfo)) {
                if (process == null) {
                    Debug.LogError($"Failed to open {BaseResultsDir}.");
                    return;
                }
            }
        }

        [MenuItem("Realm AI/Open Dashboard")]
        private static void OpenDashboard() {
            EnsureTemplatesExist();

            var envSetupPath = Path.Combine(TrainingUtilsDir, EnvSetupScript);
            var scriptPath =  Path.Combine(TrainingUtilsDir, DashboardApiScript);
            var startInfo = new ProcessStartInfo(ScriptProcess);
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.Arguments = $"{ScriptProcessArgsPreamble} \"\"{envSetupPath}\" && \"{scriptPath}\" \"{DashboardApiDir}\"\"";

            Process.Start(startInfo);
            
            Application.OpenURL($"file://{DashboardPath}");
            Debug.Log("Starting dashboard...");
        }

        private static void EnsureTemplatesExist() {
            Directory.CreateDirectory(FilesInProjectDir);
            var sourceDir = new DirectoryInfo(Path.GetFullPath(TemplatesPath));
            var targetDir = new DirectoryInfo(FilesInProjectDir);
            CopyDirectoryRecursive(sourceDir, targetDir);
        }
        
        private static void CopyDirectoryRecursive(DirectoryInfo source, DirectoryInfo target) {
            // adapted from https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?redirectedfrom=MSDN&view=net-6.0
            if (source.FullName.ToLower() == target.FullName.ToLower()) {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false) {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles()) {
                var dest = Path.Combine(target.ToString(), fi.Name);
                if (!File.Exists(dest)) {
                    Debug.Log($"Copying {target.FullName} to {fi.Name}");
                    fi.CopyTo(dest);
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyDirectoryRecursive(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
