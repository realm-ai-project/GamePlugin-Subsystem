using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    public class TrainingRunner : MonoBehaviour {
        private const string TrainingCommand = "mlagents-learn --run-id=\"Editor\" --force";
        
        [MenuItem("Realm AI/Train in Editor")]
        private static void TrainInEditor() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                
                // TODO create training runner files on initialize so they can be edited by user
                // TODO add for mac and linux
                // TODO allow configuration for: output folder, run id, custom config file, etc.?
                var runnerPath = EnsureTrainingRunnerExists();
                var command = $"start cmd /K \"{runnerPath}\"";
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/C {command}";
                Process.Start(startInfo);

                EditorApplication.EnterPlaymode();
                Debug.Log("Started processes for training in editor.");
            } else {
                Debug.Log("Train in Editor: Stop playing in the editor and try again.");
            }
        }

        private static string EnsureTrainingRunnerExists() {
            var dir = $"{Application.dataPath}/realmai";
            Directory.CreateDirectory(dir);
            var path = $"{dir}/train-editor.bat";
            if (!File.Exists(path)) {
                File.WriteAllText(path, TrainingCommand);
            }

            return path;
        }
    }
}
