using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    public class RealmRecorder : MonoBehaviour {
        [SerializeField] private Camera _recordingCamera = null;
        [SerializeField] private string _saveDirectoryPath = null;
        [SerializeField] private Vector2Int _videoResolution = new Vector2Int(480, 270);
        [SerializeField] private string _ffmpegPath = "c:\\ffmpeg\\bin\\ffmpeg.exe";
        
        void Update() {
            if (Input.GetKeyDown(KeyCode.F)) {
                StartCoroutine(FfmpegTest());
            }
        }

        IEnumerator FfmpegTest() {
            var outPath = $"{_saveDirectoryPath}/1.webm";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = _ffmpegPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"-f rawvideo -pixel_format rgb24 -video_size 480x270 -framerate 60 -i pipe: -vf vflip -y {outPath}";

            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true; // ffmpeg outputs everything to std error       


            using (Process exeProcess = Process.Start(startInfo))
            {
                if (exeProcess == null) {
                    Debug.LogError("Failed to start FFmpeg process");
                    yield break;
                }
                
                // recording
                for (int i = 0; i < 240; i++) {
                    using (var stream = new MemoryStream()) {
                        var rt = new RenderTexture(_videoResolution.x, _videoResolution.y, 0);
                        var camRt = _recordingCamera.targetTexture;
                        _recordingCamera.targetTexture = rt;
                        _recordingCamera.Render();
                        _recordingCamera.targetTexture = camRt;

                        var texture = new Texture2D(_videoResolution.x, _videoResolution.y, TextureFormat.RGB24, false);
                        var activeRt = RenderTexture.active;
                        RenderTexture.active = rt;
                        texture.ReadPixels(new Rect(0, 0, _videoResolution.x, _videoResolution.y), 0, 0);
                        texture.Apply();
                        RenderTexture.active = activeRt;

                        var bytes = texture.GetRawTextureData();
                        stream.Write(bytes, 0, bytes.Length);

                        Destroy(rt);
                        Destroy(texture);

                        var euler = _recordingCamera.transform.eulerAngles;
                        euler.x -= 0.3f;
                        euler.y += 0.1f;
                        euler.z += 0.3f;
                        _recordingCamera.transform.eulerAngles = euler;

                        stream.WriteTo(exeProcess.StandardInput.BaseStream);
                        exeProcess.StandardInput.Flush();
                    }
                    yield return null;
                }
                
                exeProcess.StandardInput.Close();
                Debug.Log("Done writing");
                
                string error = exeProcess.StandardError.ReadToEnd();
                exeProcess.WaitForExit();

                Debug.Log("OUTPUT:" + error);
            }
        }
    }
}