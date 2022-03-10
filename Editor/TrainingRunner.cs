using System;
using System.Diagnostics;
using System.IO;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    [InitializeOnLoadAttribute]
    public class TrainingRunner : MonoBehaviour {
        private static string FilesInProjectDir => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "RealmAI");
        private static string BaseResultsDir => Path.Combine(FilesInProjectDir, "Results");
        private static string DashboardPath => Path.Combine(FilesInProjectDir, "Dashboard", "index.html");
        private static string DashboardApiDir => Path.Combine(FilesInProjectDir, "Dashboard", "api");
        private static string TemplatesPath => Path.Combine("Packages", "com.realmai.unity", "Editor", "Templates");


#if UNITY_EDITOR_WIN
        private const string CommandProcess = "cmd";
#else
        private const string CommandProcess = "bash";
#endif

        private const string DefaultTrainingBuildName = "TrainingBuild";
        private const string DateTimeFormat = "yyyy-MM-dd_hh-mm-ss";

        private static Process _trainingProcess = null;

        static TrainingRunner() {
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
                var runId = $"{behaviorName}_{DateTime.Now.ToString(DateTimeFormat)}";
                var resultsDir = Path.Combine(BaseResultsDir, "Editor");

                // TODO: feels weird? saving this path so we can access it when training in editor
                SaveCurrentResultsDirectory(Path.Combine(resultsDir, runId));

                RunCommands(new[] {
                    $"realm-gui --results-dir \"{resultsDir}\" --run-id {runId} --mlagents",
                });
                RealmEditorTrainingWaitWindow.ShowWindow();
            } else {
                Debug.Log("Train in Editor: Stop playing in the editor and try again.");
            }
        }

        [MenuItem("Realm AI/Make Build and Train")]
        private static void BuildAndTrainWithBuild() {
            _TrainWithBuild(true);
        }

        [MenuItem("Realm AI/Train with Existing Build")]
        private static void TrainWithBuild() {
            _TrainWithBuild(false);
        }

        private static void _TrainWithBuild(bool makeNewBuild) {
            
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                var extension = "";
#if UNITY_EDITOR_WIN
                extension = "exe";
#elif UNITY_EDITOR_OSX
                extension = "app";
#elif UNITY_EDITOR_LINUX
                if (IntPtr.Size == 8) {
                    extension = "x86_64";
                } else {
                    extension = "x86";
                }
#else
                Debug.LogError("Build not support on this platform (but trying anyway...)");
#endif

                var title = makeNewBuild ? "Select Save Location for Build" : "Select Build Executable";
                var buildPath = EditorUtility.SaveFilePanel(title, "", DefaultTrainingBuildName, extension);
                if (string.IsNullOrEmpty(buildPath))
                    return;

                var behaviorName = GetBehaviorName() ?? FindBehaviorNameFromScene();

                if (makeNewBuild) {
                    var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, EditorUserBuildSettings.selectedStandaloneTarget, BuildOptions.None);
                    switch (buildReport.summary.result) {
                        case BuildResult.Succeeded:
                            Debug.Log($"Build for training completed after {buildReport.summary.totalTime}s, starting training process...");
                            break;
                        case BuildResult.Cancelled:
                            Debug.Log($"Build for training cancelled.");
                            return;
                        case BuildResult.Failed:
                            Debug.LogError($"Build for training failed with {buildReport.summary.totalErrors} errors");
                            return;
                        case BuildResult.Unknown:
                            Debug.LogError($"Build for training has suffered an unknown fate. Trying to train with it anyway...");
                            break;
                    }
                }

                // run scripts
                EnsureTemplatesExist();
                var runId = $"{behaviorName}_{DateTime.Now.ToString(DateTimeFormat)}";
                var resultsDir = Path.Combine(BaseResultsDir, "Build");
                
                var userSettings = RealmEditorSettings.LoadUserSettings();
                var ffmpegPathArg = "";
                if (!string.IsNullOrEmpty(userSettings.FfmpegPath)) {
                    ffmpegPathArg = $"--ffmpeg-path \"{userSettings.FfmpegPath}\"";
                }
                
                RunCommands(new[] {
                    $"realm-gui --results-dir \"{resultsDir}\" --run-id {runId} --env-path \"{buildPath}\" {ffmpegPathArg} --hyperparameter",
                });
            } else {
                Debug.Log("Build and Train: Stop playing in the editor and try again.");
            }
        }

        [MenuItem("Realm AI/Open Results Directory")]
        private static void OpenResultsDirectory() {
            if (!Directory.Exists(BaseResultsDir)) {
                Directory.CreateDirectory(BaseResultsDir);
            }
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

            RunCommands(new[] {
                "realm-gui --dashboard",
            });

            Debug.Log("Starting dashboard...");
        }

        
        [MenuItem("Realm AI/Open Documentation")]
        private static void OpenDocumentation() {
            Application.OpenURL("https://realm-ai-project.github.io/documentation/installation/");
            Debug.Log("Opening Documentation...");
        }
        
        // TODO remove this option if it is not needed anymore
        // [MenuItem("Realm AI/Kill Training Process")]
        // private static void ExitTestCommandPrompt() {
        //     try {
        //         if (_trainingProcess != null && !_trainingProcess.HasExited) {
        //             _trainingProcess.Kill();
        //         }
        //     } catch (Exception e) {
        //         Debug.LogException(e);
        //     } finally {
        //         _trainingProcess?.Dispose();
        //     }
        //
        //     _trainingProcess = null;
        // }
        
        #region Utility Functions

        private static void RunCommands(string[] commands) {
            var startInfo = new ProcessStartInfo(CommandProcess) {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            // _trainingProcess?.Dispose();
            
            _trainingProcess = new Process {StartInfo = startInfo};
            _trainingProcess.OutputDataReceived += (sender, args) => HandleTrainingProcessOutput(args.Data);
            _trainingProcess.ErrorDataReceived += (sender, args) => HandleTrainingProcessOutput(args.Data);
            try {
                if (!_trainingProcess.Start()) {
                    Debug.LogError("Failed to start training process");
                    return;
                }
            } catch (Exception e) {
                Debug.LogException(e);
                return;
            }

            var userSettings = RealmEditorSettings.LoadUserSettings();
            if (string.IsNullOrEmpty(userSettings.EnvSetupCommand)) {
                HandleTrainingProcessOutput("Note: You can add environment setup commands in the Configuration Window!");
            } else {
                _trainingProcess.StandardInput.WriteLine(userSettings.EnvSetupCommand);
            }
            
            foreach (var line in commands) {
                _trainingProcess.StandardInput.WriteLine(line);
            }

            _trainingProcess.StandardInput.WriteLine("exit");
            _trainingProcess.StandardInput.Close();
            _trainingProcess.BeginOutputReadLine();
            _trainingProcess.BeginErrorReadLine();
        }

        private static void HandleTrainingProcessOutput(string line) {
            if (string.IsNullOrEmpty(line)) {
                return;
            }

            if (line.StartsWith("realm-gui started successfully")) {
                line = line.Replace("successfully", $"<color=green>successfully</color>");
                Debug.Log($"<color=#b45cf2ff>{line}</color>");
                // _trainingProcess?.Dispose();
                // _trainingProcess = null;
            } else {
                Debug.Log($"<color=#b45cf2ff>{line}</color>");
            }
        }
        
        private static string GetBehaviorName() {
            var settings = RealmEditorSettings.LoadProjectSettings();
            if (string.IsNullOrEmpty(settings.PlayerPrefabGuid)) {
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
            var settings = RealmEditorSettings.LoadUserSettings();
            settings.CurrentResultsDirectory = directory;
            RealmEditorSettings.SaveUserSettings(settings);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                SaveCurrentResultsDirectory("");
            }
        }

        private static void EnsureTemplatesExist() {
            Directory.CreateDirectory(FilesInProjectDir);
            var sourceDir = new DirectoryInfo(Path.GetFullPath(TemplatesPath));
            var targetDir = new DirectoryInfo(FilesInProjectDir);
            CopyDirectoryRecursive(sourceDir, targetDir);
        }

        private static void CopyDirectoryRecursive(DirectoryInfo source, DirectoryInfo target) {
            // adapted from https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?redirectedfrom=MSDN&view=net-6.0
            if (string.Equals(source.FullName, target.FullName, StringComparison.CurrentCultureIgnoreCase)) {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (!Directory.Exists(target.FullName)) {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (var fi in source.GetFiles()) {
                var dest = Path.Combine(target.ToString(), fi.Name);
                if (!File.Exists(dest)) {
                    Debug.Log($"Copying {target.FullName} to {fi.Name}");
                    fi.CopyTo(dest);
                }
            }

            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories()) {
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyDirectoryRecursive(diSourceSubDir, nextTargetSubDir);
            }
        }

        #endregion

    }
}
