using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    public class TrainingRunner : MonoBehaviour {
        private static string DirInProject => $"{Application.dataPath}/realmai/Training";
        private const string TemplatesPath = "Packages/com.realmai.unity/Editor/Templates";
        
        private const string EditorTrainingScript = "train-editor.bat";
        private const string BuildTrainingScript = "train-build.bat";
        private const string EditorTrainingConfig = "train-editor-config.yaml";
        private const string BuildTrainingConfig = "train-build-config.yaml";

        private const string DefaultEditorTrainingName = "Editor";
        private const string DefaultTrainingBuildName = "TrainingBuild";
        
        // TODO create training runner files on initialize so they can be edited by user
        // TODO add for mac and linux
        // TODO allow configuration for: output folder, run id, custom config file, etc.?
        [MenuItem("Realm AI/Train in Editor")]
        private static void TrainInEditor() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                EnsureTrainingScriptsExist();
                var scriptPath = $"{DirInProject}/{EditorTrainingScript}";
                var configPath = $"{DirInProject}/{EditorTrainingConfig}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/K \"\"{scriptPath}\" \"{configPath}\"\"";
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
                var buildName = Path.GetFileNameWithoutExtension(buildPath);
                if (string.IsNullOrEmpty(buildPath))
                    return;
                
                Debug.Log("Build started...");
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows, BuildOptions.None);

                Debug.Log("Build for training completed, starting training process...");
                
                EnsureTrainingScriptsExist();
                var scriptPath = $"{DirInProject}/{BuildTrainingScript}";
                var configPath = $"{DirInProject}/{BuildTrainingConfig}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"/K \"\"{scriptPath}\" \"{configPath}\" \"{buildPath}\" \"{buildName}\"\"";
                Debug.Log(startInfo.Arguments);
                Process.Start(startInfo);
            } else {
                Debug.Log("Build and Train: Stop playing in the editor and try again.");
            }
        }

        private static void EnsureTrainingScriptsExist() {
            Directory.CreateDirectory(DirInProject);
            if (!File.Exists($"{DirInProject}/{EditorTrainingScript}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{EditorTrainingScript}", $"{DirInProject}/{EditorTrainingScript}");
            }
            
            if (!File.Exists($"{DirInProject}/{BuildTrainingScript}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{BuildTrainingScript}", $"{DirInProject}/{BuildTrainingScript}");
            }
            
            if (!File.Exists($"{DirInProject}/{EditorTrainingConfig}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{EditorTrainingConfig}", $"{DirInProject}/{EditorTrainingConfig}");
            }

            if (!File.Exists($"{DirInProject}/{BuildTrainingConfig}")) {
                FileUtil.CopyFileOrDirectory($"{TemplatesPath}/{BuildTrainingConfig}", $"{DirInProject}/{BuildTrainingConfig}");
            }
        }
    }
}
