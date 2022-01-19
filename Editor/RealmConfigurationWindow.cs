using System;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmAI {
    public class RealmConfigurationWindow : EditorWindow {
        private static string RealmAiModulePath => Path.Combine("Packages", "com.realmai.unity", "Runtime", "Realm AI Module.prefab");

        private GameObject _playerPrefab = null;
        private GameObject _realmModuleRoot = null;


        private Vector2 _scrollPos = Vector2.zero;

        [MenuItem("Realm AI/Open Configuration Window")]
        public static void ShowWindow() {
            GetWindow(typeof(RealmConfigurationWindow), false, "Realm AI Configuration", true);
        }

        private void LoadPlayerPrefabFromSettings() {
            var settings = RealmEditorSettings.LoadProjectSettings();
            if (!string.IsNullOrEmpty(settings.PlayerPrefabGuid)) {
                _playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(settings.PlayerPrefabGuid));
            } else {
                _playerPrefab = null;
            }
        }

        private void SavePlayerPrefabToSettings() {
            var settings = RealmEditorSettings.LoadProjectSettings();

            if (_playerPrefab != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_playerPrefab, out var guid, out long localId)) {
                settings.PlayerPrefabGuid = guid;
            } else {
                settings.PlayerPrefabGuid = "";
            }

            RealmEditorSettings.SaveProjectSettings(settings);
        }

        private void Awake() {
            LoadPlayerPrefabFromSettings();
        }

        private void OnGUI() {
            InstallRealmModule();

            if (_playerPrefab != null && _realmModuleRoot != null) {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                ConfigureRealmModule();
                EditorGUILayout.EndScrollView();
            }

            ConfigureUserSettings();
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

        private void ConfigureRealmModule() {
            if (_realmModuleRoot == null) {
                Debug.LogError("The player prefab should be selected first.");
                return;
            }

            HLine();

            // TODO null checks
            var behaviorParameters = new SerializedObject(_realmModuleRoot.GetComponentInChildren<BehaviorParameters>());
            var decisionRequester = new SerializedObject(_realmModuleRoot.GetComponentInChildren<DecisionRequester>());
            var realmAgent = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmAgent>());
            var realmSensor = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmSensorComponent>());
            var gridSensor = new SerializedObject(_realmModuleRoot.GetComponentInChildren<GridSensorComponent>());
            var realmActuator = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmActuatorComponent>());
            var realmOwl = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmOwl>());
            var realmRecorder = new SerializedObject(_realmModuleRoot.GetComponentInChildren<RealmRecorder>());

            var spaceBetweenProperties = 8;
            // GENERAL
            GUILayout.Label("General", EditorStyles.largeLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.LabelField("This section contains general setup relating to game environment and gameplay loop.", EditorStyles.wordWrappedLabel);

                EditorGUILayout.PropertyField(behaviorParameters.FindProperty("m_BehaviorName"), new GUIContent("Behavior Name"));
                EditorGUILayout.HelpBox("Behavior Name is an arbitrary name for the player. " +
                                        "Any neural network models and data generated will be labeled with this behavior name.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                behaviorParameters.ApplyModifiedProperties();

                EditorGUILayout.PropertyField(realmAgent.FindProperty("_resetFunction"), new GUIContent("Reset Function"));
                EditorGUILayout.HelpBox("Provide a function that we can call to reset the game environment to its initial state.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(realmAgent.FindProperty("_rewardFunction"), new GUIContent("Reward Function"));
                EditorGUILayout.HelpBox("Provide a function that we can call to get the current score achieved by the player.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(realmAgent.FindProperty("_gameOverFunction"), new GUIContent("Game Over Function"));
                EditorGUILayout.HelpBox("Provide a function that we can call which will return true when the current game is over.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                realmAgent.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel--;
            HLine();

            // OBSERVATIONS
            GUILayout.Label("Observations", EditorStyles.largeLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.LabelField("This section contains setup to help the computer see where it is in the game world and what's around it.", EditorStyles.wordWrappedLabel);

                EditorGUILayout.PropertyField(realmSensor.FindProperty("_positionFunction"), new GUIContent("Position Function"));
                EditorGUILayout.HelpBox("Provide a function that returns the current position of the player.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                realmSensor.ApplyModifiedProperties();

                EditorGUILayout.LabelField("The computer will detect objects in its surroundings by detecting physics colliders in a grid.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.PropertyField(gridSensor.FindProperty("m_ColliderMask"), new GUIContent("Collider Mask"));
                EditorGUILayout.HelpBox("Select all layers which have important colliders to know about.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(gridSensor.FindProperty("m_DetectableTags"), new GUIContent("Detectable Tags"));
                EditorGUILayout.HelpBox("Enter the tags on the colliders that should be detectable. " +
                                        "Colliders representing different types of objects should have different tags.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(gridSensor.FindProperty("m_GridSize"), new GUIContent("Grid Cell Size"));
                EditorGUILayout.HelpBox("The number of cells along each dimension of the grid. " +
                                        "This should make the grid large enough for the computer to play the game properly.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(gridSensor.FindProperty("m_CellScale"), new GUIContent("Grid Cell Scale"));
                EditorGUILayout.HelpBox("The size of each cell in the grid. " +
                                        "The smaller the cell size, the more precisely the computer can play the game. " +
                                        "This should be small enough so the computer can play well, but not too small.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(gridSensor.FindProperty("m_ShowGizmos"), new GUIContent("Show Grid Gizmos"));
                EditorGUILayout.HelpBox("You can show gizmos for the grid for testing and debugging.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                if (gridSensor.FindProperty("m_ShowGizmos").boolValue) {
                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_GizmoYOffset"), new GUIContent("Grid Gizmo Y Offset"));
                    EditorGUILayout.Space(spaceBetweenProperties);
                    EditorGUILayout.PropertyField(gridSensor.FindProperty("m_DebugColors"), new GUIContent("Grid Debug Colors"));
                    EditorGUILayout.HelpBox("For each detectable tag, assign a color. When a cell detects a collider with a detectable tag, " +
                                            "the cell in the gizmo will turn into the corresponding color.", MessageType.None);
                    EditorGUILayout.Space(spaceBetweenProperties);
                }

                gridSensor.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel--;
            HLine();

            // Actions
            GUILayout.Label("Controls", EditorStyles.largeLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.LabelField("This section contains setup to allow the computer to input commands and control the player. " +
                                           "Actions are separated into continuous and discrete.", EditorStyles.wordWrappedLabel);

                EditorGUILayout.PropertyField(realmActuator.FindProperty("_continuousActionSpecs"), new GUIContent("Continuous Actions"));
                EditorGUILayout.HelpBox("Continuous actions are actions that can be represented by a float input, such as a joystick axis. " +
                                        "For each continuous action that the player can take, provide a function to call when the computer wants " +
                                        "set a new input value for that action (callback) and a function to call to get a input value from a human player (heuristic, for debugging).",
                    MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);

                EditorGUILayout.PropertyField(realmActuator.FindProperty("_discreteActionSpecs"), new GUIContent("Discrete Actions"));
                EditorGUILayout.HelpBox("Discrete actions are actions that can be represented by a boolean or integer input, such as a button press. " +
                                        "For each discrete action that the player can take, provide a function to call when the computer wants " +
                                        "set a new input value for that action (callback) and a function to call to get a input value from a human player (heuristic, for debugging).",
                    MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                realmActuator.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel--;
            HLine();

            // Analytics
            GUILayout.Label("Analytics", EditorStyles.largeLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.LabelField("This section contains setup to allow the stats and other analytics data to be recorded.", EditorStyles.wordWrappedLabel);

                EditorGUILayout.PropertyField(realmSensor.FindProperty("_positionRecordInterval"), new GUIContent("Position Record Interval"));
                EditorGUILayout.HelpBox("For generating heatmaps, how long (in seconds) should we wait between recording each point of the player's position?.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                realmSensor.ApplyModifiedProperties();

                EditorGUILayout.PropertyField(realmRecorder.FindProperty("_ffmpegPath"), new GUIContent("FFMPEG Path"));
                EditorGUILayout.HelpBox("Optionally, provide a path to an ffmpeg executable to record video replays.", MessageType.None);
                EditorGUILayout.Space(spaceBetweenProperties);
                if (!string.IsNullOrEmpty(realmRecorder.FindProperty("_ffmpegPath").stringValue)) {
                    EditorGUILayout.PropertyField(realmRecorder.FindProperty("_videoResolution"), new GUIContent("Video Resolution"));
                    EditorGUILayout.HelpBox("The resolution for video replays.", MessageType.None);
                    EditorGUILayout.Space(spaceBetweenProperties);
                    EditorGUILayout.PropertyField(realmRecorder.FindProperty("_videosPerMillionSteps"), new GUIContent("Videos Per Million Steps"));
                    EditorGUILayout.HelpBox("The frequency of video replays. Generally, leave this at a low number unless you need a lot of video replays.", MessageType.None);
                    EditorGUILayout.Space(spaceBetweenProperties);
                }

                realmRecorder.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel--;
        }
        
        private void ConfigureUserSettings() {
            HLine();

            var userSettings = RealmEditorSettings.LoadUserSettings();

            var userSettingsChanged = false;
            var spaceBetweenProperties = 8;
            GUILayout.Label("User Settings", EditorStyles.largeLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.LabelField("Environment Setup Command", EditorStyles.boldLabel);
                var envSetupCommand = EditorGUILayout.TextField(userSettings.EnvSetupCommand);
                EditorGUILayout.HelpBox("Optionally, provide command(s) to run to setup the current environment " +
                                        "before running any of the training processes. For example, add a command to activate your " +
                                        "Python environment here.", MessageType.None);
                if (envSetupCommand != userSettings.EnvSetupCommand) {
                    userSettingsChanged = true;
                    userSettings.EnvSetupCommand = envSetupCommand;
                }
                EditorGUILayout.Space(spaceBetweenProperties);
            }
            EditorGUI.indentLevel--;

            if (userSettingsChanged) {
                RealmEditorSettings.SaveUserSettings(userSettings);
            }
        }
        
        private void HLine() {
            // draw a horizontal line with slight spacing above and below
            var horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 6, 6);
            horizontalLine.fixedHeight = 1;

            var c = GUI.backgroundColor;
            GUI.color = Color.black;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }
    }
}