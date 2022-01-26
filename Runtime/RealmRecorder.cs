using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RealmAI {
    public class RealmRecorder : MonoBehaviour {
        [SerializeField] private RealmAgent _realmAgent = null;
        [SerializeField] private Camera _recordingCamera = null;
        [SerializeField] private Vector2Int _videoResolution = new Vector2Int(480, 270);
        [SerializeField] private float _videosPerMillionSteps = 100;

        private const string RecordingExtension = "webm";
        
        private int _episodeNumber = 0;
        private int _totalStepsCompleted = 0;
        private float _currentReward = 0;
        
        private int _recordedVideoCount = 0;
        
        private bool _recordingEpisode = false;
        private string _recordingOutPath = "";
        private Process _recordingProcess = null;

        private string _ffmpegPath = "";

        private void Awake() {
            GetFfmpegPathFromArgs();
        }

        private void GetFfmpegPathFromArgs() {
            if (!Application.isEditor) {
                var args = System.Environment.GetCommandLineArgs();

                // parse from command line arguments
                var parseFfmpegPath = false;
                foreach (var arg in args) {
                    if (parseFfmpegPath && _ffmpegPath == "") {
                        _ffmpegPath = arg;
                        parseFfmpegPath = false;
                    } else if (arg == "-ffmpeg-path") {
                        parseFfmpegPath = true;
                    }
                }
                Debug.Log(string.Join(" ", args));
            }


#if UNITY_EDITOR
            // when training in the editor, the training runner should have identified a directory for us:
            // TODO very temp solution for getting save folder from python gui
            var settings = RealmEditorSettings.LoadUserSettings();
            if (!string.IsNullOrEmpty(settings.FfmpegPath)) {
                _ffmpegPath = settings.FfmpegPath;
            }
#endif
        }

        public void StartEpisode(int episodeNumber, int totalStepsCompleted) {
            if (!isActiveAndEnabled)
                return;
            
            _episodeNumber = episodeNumber;
            _totalStepsCompleted = totalStepsCompleted;
            _currentReward = 0;
            
            if (_recordingEpisode) {
                StopRecording();
                _recordingEpisode = false;
            }
            
            if (ShouldRecordVideo(totalStepsCompleted) || Input.GetKey(KeyCode.R)) {
                _recordingEpisode = true;
                StartRecording();
            }
        }

        public void RecordReward(float reward) {
            _currentReward = reward;
        }

        private void Update() {
            if (_recordingEpisode) {
                RecordFrame();
            }
        }

        private void OnDestroy() {
            if (_recordingEpisode) {
                StopRecording();
                _recordingEpisode = false;
            }
        }

        private void OnApplicationQuit() {
            if (_recordingEpisode) {
                StopRecording();
                _recordingEpisode = false;
            }
        }

        private bool ShouldRecordVideo(int totalStepsCompleted) {
            var stepRatio = totalStepsCompleted / 1e6f;
            var videoRatio = _recordedVideoCount / _videosPerMillionSteps;

            if (videoRatio < stepRatio) {
                return true;
            }

            return false;
        }

        private void StartRecording() {
            if (string.IsNullOrEmpty(_ffmpegPath)) {
                _recordingEpisode = false;
                return;
            }
            
            var saveDirectory = Path.Combine(_realmAgent.SaveDirectory, "Videos");
            Directory.CreateDirectory(saveDirectory);
            // TODO resolve filename conflicts across multiple environments
            long count = 0;
            var nameOk = false;
            while (!nameOk) {
                _recordingOutPath = Path.Combine(saveDirectory, $"{_totalStepsCompleted + count}");

                nameOk = !File.Exists($"{_recordingOutPath}.{RecordingExtension}") && !File.Exists($"{_recordingOutPath}-temp.{RecordingExtension}");
                count++;
            }

            // frame rate
            // TODO: determine appropriate framerate for recording when training in editor (just try to track current framerate?)
            var framerate = Time.captureFramerate / Time.timeScale;
            if (framerate == 0) {
                if (Application.targetFrameRate != -1) {
                    framerate = Application.targetFrameRate;
                } else {
                    framerate = 60;
                }
            }

            // start ffmpeg process
            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = _ffmpegPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"-f rawvideo -pixel_format rgb24 -video_size {_videoResolution.x}x{_videoResolution.y} -framerate {framerate} -i pipe: -vf vflip -y \"{_recordingOutPath}-temp.{RecordingExtension}\"";
            // startInfo.Arguments = $"-f image2pipe -framerate {framerate} -i pipe: -vf vflip -y \"{_recordingOutPath}-temp.{RecordingExtension}\"";

            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true; // ffmpeg outputs everything to std error    

            try {
                _recordingProcess = Process.Start(startInfo);
            } catch (Exception e) {
                Debug.LogException(e);
                _recordingProcess = null;
            }

            if (_recordingProcess == null) {
                Debug.LogError("Failed to start FFmpeg process");
                _recordingEpisode = false;
            }
        }

        private void RecordFrame() {
            if (_recordingProcess == null || _recordingProcess.HasExited) {
                Debug.LogWarning("Trying to record video replay frame when recording process is not running");
                return;
            }
            
            var cameraWasEnabled = _recordingCamera.enabled;
            _recordingCamera.enabled = true;
            
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

            using (var stream = new MemoryStream()) {
                var bytes = texture.GetRawTextureData();
                // if (_gridSensor.Sensors != null && _gridSensor.Sensors.Length > 0) {
                //     bytes = _gridSensor.Sensors[0].GetCompressedObservation();
                // }
                stream.Write(bytes, 0, bytes.Length);

                Destroy(rt);
                Destroy(texture);
                try {
                    stream.WriteTo(_recordingProcess.StandardInput.BaseStream);
                    _recordingProcess.StandardInput.Flush();
                } catch (IOException e) {
                    Debug.LogException(e);
                    _recordingEpisode = false;
                    CancelRecording();
                }
            }
            _recordingCamera.enabled = cameraWasEnabled;
        }

        private void StopRecording() {
            if (_recordingProcess == null || _recordingProcess.HasExited) {
                Debug.LogWarning("Trying to record video replay frame when recording process is not running");
                return;
            }
            
            try {
                _recordingProcess.StandardInput.Close();
                string output = _recordingProcess.StandardError.ReadToEnd();
                Debug.Log("Recording completed: ffmpeg output:" + output);
                _recordingProcess.WaitForExit();
            } finally {
                _recordingProcess.Dispose();
            }
            
            _recordedVideoCount++;
            ModifyMetadata();
        }


        private void CancelRecording() {
            if (_recordingProcess == null || _recordingProcess.HasExited) {
                Debug.LogWarning("Trying to record video replay frame when recording process is not running");
                return;
            }
            
            try {
                _recordingProcess.StandardInput.Close();
                string output = _recordingProcess.StandardError.ReadToEnd();
                Debug.Log("Recording cancelled: ffmpeg output:" + output);
                _recordingProcess.WaitForExit();
            } finally {
                _recordingProcess.Dispose();
            }
        }
        
        private void ModifyMetadata() {
            var metadata = $"-metadata REALMAI_EPISODE={_episodeNumber} -metadata REALMAI_REWARD={_currentReward}";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = _ffmpegPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"-i \"{_recordingOutPath}-temp.{RecordingExtension}\" -c copy {metadata} \"{_recordingOutPath}.{RecordingExtension}\" -y";

            startInfo.RedirectStandardError = true; // ffmpeg outputs everything to std error

            using (var process = Process.Start(startInfo)) {
                if (process == null) {
                    Debug.LogError("Failed to start FFmpeg process");
                    return;
                }

                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Debug.Log("Set recording metadata: ffmpeg output:" + output);
            }

            File.Delete($"{_recordingOutPath}-temp.{RecordingExtension}");
        }
    }
}