using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmAI {
    public class RealmConfigurationWindow : EditorWindow {

        private const string RealmAiModulePath = "Packages/com.realmai.unity/Runtime/Realm AI Module.prefab";

        private enum Page {
            General,
            Environment,
            Observations,
            Actions,
            Analytics,
            Video,
        }

        private GameObject _playerPrefab = null;
        private GameObject _realmModuleRoot = null;

        private Vector2 _scrollPos = Vector2.zero;
        private Page _page = Page.General;

        [MenuItem("Realm AI/Open Configuration Window")]
        public static void ShowWindow() {
            GetWindow(typeof(RealmConfigurationWindow));
        }

        private void LoadPlayerPrefabFromSettings() {
            var settings = RealmEditorSettings.LoadSettings();
            if (settings != null && !string.IsNullOrEmpty(settings.PlayerPrefabGuid)) {
                _playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(settings.PlayerPrefabGuid));
            } else {
                _playerPrefab = null;
            }
        }

        private void SavePlayerPrefabToSettings() {
            var settings = RealmEditorSettings.LoadSettings() ?? new RealmEditorSettings();

            if (_playerPrefab != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_playerPrefab, out var guid, out long localId)) {
                settings.PlayerPrefabGuid = guid;
            } else {
                settings.PlayerPrefabGuid = "";
            }

            RealmEditorSettings.SaveSettings(settings);
        }

        private void Awake() {
            // TODO does this work on assembly reload?
            LoadPlayerPrefabFromSettings();
        }

        private void OnGUI() {
            InstallRealmModule();

            if (_playerPrefab != null && _realmModuleRoot != null) {
                GUILayout.BeginHorizontal();
                DrawNavButtons();
                GUILayout.EndHorizontal();
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                ConfigureRealmModule();
                EditorGUILayout.EndScrollView();
            }

        }

        private void InstallRealmModule() {
            // TODO handle player objects as scene objects
            _realmModuleRoot = null;

            var selectedPlayerPrefab = EditorGUILayout.ObjectField("Player Prefab", _playerPrefab, typeof(GameObject), false) as GameObject;
            if (_playerPrefab != selectedPlayerPrefab) {
                _playerPrefab = selectedPlayerPrefab;
                SavePlayerPrefabToSettings();
            }

            if (_playerPrefab == null) {
                GUILayout.Label("Choose the prefab that represents the player.", EditorStyles.helpBox);
                return;
            }

            var playerPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_playerPrefab);
            if (playerPrefabPath == RealmAiModulePath) {
                GUILayout.Label("Cannot use Realm AI Module as player prefab", EditorStyles.helpBox);
                return;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.assetPath != playerPrefabPath) {
                if (GUILayout.Button("Open player prefab for Edit")) {
                    AssetDatabase.OpenAsset(_playerPrefab);
                }

                GUILayout.Label("Open the player prefab in prefab edit mode to configure it.", EditorStyles.helpBox);
                return;
            }

            var root = prefabStage.prefabContentsRoot;
            var realmAgent = root.GetComponentInChildren<RealmAgent>();
            if (realmAgent == null) {
                if (GUILayout.Button("Add Realm AI Module")) {
                    var realmAiModulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RealmAiModulePath);
                    PrefabUtility.InstantiatePrefab(realmAiModulePrefab, root.transform);
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }

                GUILayout.Label("Start by adding an instance of the Realm AI Module prefab as a child of the player prefab.", EditorStyles.helpBox);
                return;
            }

            _realmModuleRoot = realmAgent.gameObject;
        }

        private void DrawNavButtons() {
            if (GUILayout.Button("General")) {
                _page = Page.General;
            }

            if (GUILayout.Button("Environment")) {
                _page = Page.Environment;
            }

            if (GUILayout.Button("Observations")) {
                _page = Page.Observations;
            }

            if (GUILayout.Button("Actions")) {
                _page = Page.Actions;
            }

            if (GUILayout.Button("Analytics")) {
                _page = Page.Analytics;
            }

            if (GUILayout.Button("Video")) {
                _page = Page.Video;
            }
        }

        private void ConfigureRealmModule() {
            if (_realmModuleRoot == null) {
                Debug.LogError("The player prefab should be selected first.");
                return;
            }

            // TODO null checks
            var behaviorParameters = new SerializedObject(_realmModuleRoot.GetComponentInChildren<BehaviorParameters>());
            var decisionRequester = new SerializedObject(_realmModuleRoot.GetComponentInChildren<DecisionRequester>());
            var realmAgent = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmAgent>());
            var realmSensor = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmSensorComponent>());
            var gridSensor = new SerializedObject(_realmModuleRoot.GetComponentInChildren<GridSensorComponent>());
            var realmActuator = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmActuatorComponent>());
            var realmOwl = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmOwl>());
            var realmRecorder = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmRecorder>());

            switch (_page) {
                case Page.General:
                    EditorGUILayout.PropertyField(behaviorParameters.FindProperty("m_BehaviorName"), new GUIContent("Behavior Name"));
                    behaviorParameters.ApplyModifiedProperties();

                    EditorGUILayout.PropertyField(decisionRequester.FindProperty("DecisionPeriod"), new GUIContent("Decision Period"));
                    decisionRequester.ApplyModifiedProperties();
                    break;
                case Page.Environment:
                    EditorGUILayout.PropertyField(realmAgent.FindProperty("_episodeReset"), new GUIContent("Episode Reset Event"));
                    EditorGUILayout.PropertyField(realmAgent.FindProperty("_rewardFunction"), new GUIContent("Reward Function"));
                    EditorGUILayout.PropertyField(realmAgent.FindProperty("_gameOverFunction"), new GUIContent("Game Over Function"));
                    realmAgent.ApplyModifiedProperties();

                    break;
                case Page.Observations:
                    EditorGUILayout.PropertyField(realmSensor.FindProperty("_positionFunction"), new GUIContent("Position Function"));
                    realmSensor.ApplyModifiedProperties();

                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_CellScale"), new GUIContent("Grid Sensor Cell Scale"));
                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_GridSize"), new GUIContent("Grid Sensor Cell Size"));
                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_DetectableTags"), new GUIContent("Grid Sensor Detectable Tags"));
                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_ColliderMask"), new GUIContent("Grid Sensor Collider Mask"));
                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_ShowGizmos"), new GUIContent("Grid Sensor Show Gizmos"));
                    if (gridSensor.FindProperty("m_ShowGizmos").boolValue) {
                        EditorGUILayout.PropertyField(gridSensor.FindProperty("m_GizmoYOffset"), new GUIContent("Grid Sensor Gizmo Y Offset"));
                        EditorGUILayout.PropertyField(gridSensor.FindProperty("m_DebugColors"), new GUIContent("Grid Sensor Debug Colors"));
                    }

                    gridSensor.ApplyModifiedProperties();
                    break;
                case Page.Actions:
                    EditorGUILayout.PropertyField(realmActuator.FindProperty("_continuousActionSpecs"), new GUIContent("Continuous Actions"));
                    EditorGUILayout.PropertyField(realmActuator.FindProperty("_discreteActionSpecs"), new GUIContent("Discrete Actions"));
                    realmActuator.ApplyModifiedProperties();
                    break;
                case Page.Analytics:
                    EditorGUILayout.PropertyField(realmSensor.FindProperty("_positionRecordInterval"), new GUIContent("Position Record Interval"));
                    realmSensor.ApplyModifiedProperties();
                    break;
                case Page.Video:
                    EditorGUILayout.PropertyField(realmRecorder.FindProperty("_ffmpegPath"), new GUIContent("FFMPEG Path"));
                    EditorGUILayout.PropertyField(realmRecorder.FindProperty("_videoResolution"), new GUIContent("Video Resolution"));
                    EditorGUILayout.PropertyField(realmRecorder.FindProperty("_videosPerMillionSteps"), new GUIContent("Videos Per Million Steps"));
                    realmRecorder.ApplyModifiedProperties();
                    break;
            }
        }
    }
}