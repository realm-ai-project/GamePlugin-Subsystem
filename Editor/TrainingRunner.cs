using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    public class TrainingRunner : MonoBehaviour {
        private const string EditorTrainingScript = "train-editor.bat";
        private const string EditorTrainingCommand = "mlagents-learn --run-id=\"Editor\" --force";
        
        private const string BuildTrainingScript = "train-build.bat";
        private const string BuildTrainingCommand = "mlagents-learn --run-id=\"Build\" --env=%1 --force";
        private const string DefaultTrainingBuildName = "TrainingBuild.exe";
        
        // TODO create training runner files on initialize so they can be edited by user
        // TODO add for mac and linux
        // TODO allow configuration for: output folder, run id, custom config file, etc.?
        [MenuItem("Realm AI/Train in Editor")]
        private static void TrainInEditor() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                var scriptsDir = EnsureTrainingScriptsExist();
                var scriptPath = $"{scriptsDir}/{EditorTrainingScript}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/C start \"Training in Editor\" \"{scriptPath}\"";
                Process.Start(startInfo);

                // TODO this would need to wait for trainer to be ready before playing in the editor.
                EditorApplication.EnterPlaymode();
                Debug.Log("Started processes for training in editor.");
            } else {
                Debug.Log("Train in Editor: Stop playing in the editor and try again.");
            }
        }
        
        [MenuItem("Realm AI/Build and Train")]
        private static void TrainWithBuild() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                string buildPath = EditorUtility.SaveFilePanel("Choose Location of Built Game", "", DefaultTrainingBuildName, "exe");
                Debug.Log(buildPath);
                if (string.IsNullOrEmpty(buildPath))
                    return;
                
                Debug.Log("Build started...");
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows, BuildOptions.None);

                Debug.Log("Build for training completed, starting training process...");
                
                var scriptsDir = EnsureTrainingScriptsExist();
                var scriptPath = $"{scriptsDir}/{BuildTrainingScript}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/C start \"Training with Build\" \"{scriptPath}\" {buildPath}";
                Debug.Log(startInfo.Arguments);
                Process.Start(startInfo);
            } else {
                Debug.Log("Train in Editor: Stop playing in the editor and try again.");
            }
        }

        private static string EnsureTrainingScriptsExist() {
            var dir = $"{Application.dataPath}/realmai";
            Directory.CreateDirectory(dir);
            if (!File.Exists($"{dir}/{EditorTrainingScript}")) {
                File.WriteAllText($"{dir}/{EditorTrainingScript}", EditorTrainingCommand);
            }
            
            if (!File.Exists($"{dir}/{BuildTrainingScript}")) {
                File.WriteAllText($"{dir}/{BuildTrainingScript}", BuildTrainingCommand);
            }

            return dir;
        }
    }
}
