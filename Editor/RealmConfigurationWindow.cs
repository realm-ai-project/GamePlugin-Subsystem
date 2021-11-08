using RealmAI;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RealmConfigurationWindow : EditorWindow {

    private const string RealmAiModulePath = "Packages/com.realmai.unity/Runtime/Realm AI Module.prefab";
    
    private GameObject _playerPrefab = null;
    private bool _generalOptionsFoldout = true;
    private bool _playerSetupFoldout = true;
    private bool _analyticsFoldout = true;
    private bool _videoFoldout = true;
    
    private Vector2 _scrollPos = Vector2.zero;
    
    [MenuItem ("Realm AI/Open Configuration Window")]
    public static void  ShowWindow () {
        GetWindow(typeof(RealmConfigurationWindow));
    }
    
    private void OnGUI () {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        var message = InstallRealmModule();
        GUILayout.Label(message, EditorStyles.helpBox);
        EditorGUILayout.EndScrollView();
    }

    private string InstallRealmModule() {
        // TODO handle player objects as scene objects
        _playerPrefab = EditorGUILayout.ObjectField("Player Prefab", _playerPrefab, typeof(GameObject), false) as GameObject;
        if (_playerPrefab == null) {
            return "Choose the prefab that represents the player.";
        }

        var playerPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_playerPrefab);
        if (playerPrefabPath == RealmAiModulePath) {
            return "Cannot use Realm AI Module as player prefab";
        }

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage == null || prefabStage.assetPath != playerPrefabPath) {
            if (GUILayout.Button("Open player prefab for Edit")) {
                AssetDatabase.OpenAsset(_playerPrefab);
            }
            
            return "Open the player prefab in prefab edit mode to configure it.";
        }

        var root = prefabStage.prefabContentsRoot;
        var realmAgent = root.GetComponentInChildren<RealmAgent>();
        if (realmAgent == null) {
            if (GUILayout.Button("Add Realm AI Module")) {
                var realmAiModulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RealmAiModulePath);
                var realmAiModule = PrefabUtility.InstantiatePrefab(realmAiModulePrefab, root.transform);
                
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
            
            return "Start by adding an instance of the Realm AI Module prefab as a child of the player prefab.";
        }
        
        return ConfigureRealmModule(root, realmAgent.gameObject);
    }

    private string ConfigureRealmModule(GameObject root, GameObject realmModuleRoot) {
        var behaviorParameters = new SerializedObject(realmModuleRoot.GetComponentInChildren<BehaviorParameters>());
        var decisionRequester = new SerializedObject(realmModuleRoot.GetComponentInChildren<DecisionRequester>());
        var realmAgent = new SerializedObject(realmModuleRoot.GetComponentInChildren<RealmAgent>());
        var realmSensor = new SerializedObject(realmModuleRoot.GetComponentInChildren<RealmSensorComponent>());
        var gridSensor = new SerializedObject(realmModuleRoot.GetComponentInChildren<GridSensorComponent>());
        var realmActuator = new SerializedObject(realmModuleRoot.GetComponentInChildren<RealmActuatorComponent>());
        var realmOwl = new SerializedObject(realmModuleRoot.GetComponentInChildren<RealmOwl>());
        var realmRecorder = new SerializedObject(realmModuleRoot.GetComponentInChildren<RealmRecorder>());

        // TODO null checks

        _generalOptionsFoldout = EditorGUILayout.Foldout(_generalOptionsFoldout, "General Options");
        if (_generalOptionsFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(behaviorParameters.FindProperty("m_BehaviorName"), new GUIContent("Behavior Name"));
            behaviorParameters.ApplyModifiedProperties();
            
            EditorGUILayout.PropertyField(decisionRequester.FindProperty("DecisionPeriod"), new GUIContent("Decision Period"));
            decisionRequester.ApplyModifiedProperties();
            
            EditorGUI.indentLevel--;
        }
            
        _playerSetupFoldout = EditorGUILayout.Foldout(_playerSetupFoldout, "Player Setup");
        if (_playerSetupFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(realmAgent.FindProperty("_episodeReset"), new GUIContent("Episode Reset Event"));
            EditorGUILayout.PropertyField(realmAgent.FindProperty("_rewardFunction"), new GUIContent("Reward Function"));
            EditorGUILayout.PropertyField(realmAgent.FindProperty("_gameOverFunction"), new GUIContent("Game Over Function"));
            realmAgent.ApplyModifiedProperties();
            
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
            
            EditorGUILayout.PropertyField(realmActuator.FindProperty("_continuousActionSpecs"), new GUIContent("Continuous Actions"));
            EditorGUILayout.PropertyField(realmActuator.FindProperty("_discreteActionSpecs"), new GUIContent("Discrete Actions"));
            realmActuator.ApplyModifiedProperties();
            EditorGUI.indentLevel--;
        }

        _analyticsFoldout = EditorGUILayout.Foldout(_analyticsFoldout, "Analytics");
        if (_analyticsFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(realmSensor.FindProperty("_positionRecordInterval"), new GUIContent("Position Record Interval"));
            realmSensor.ApplyModifiedProperties();
            EditorGUI.indentLevel--;
        }
        
        _videoFoldout = EditorGUILayout.Foldout(_videoFoldout, "Frames");
        if (_videoFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(realmRecorder.FindProperty("_ffmpegPath"), new GUIContent("FFMPEG Path"));
            EditorGUILayout.PropertyField(realmRecorder.FindProperty("_videoResolution"), new GUIContent("Video Resolution"));
            EditorGUILayout.PropertyField(realmRecorder.FindProperty("_videosPerMillionSteps"), new GUIContent("Videos Per Million Steps"));
            realmRecorder.ApplyModifiedProperties();
            EditorGUI.indentLevel--;
        }

        return "OK";
    }
}
