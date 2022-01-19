using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealmAI {
#if UNITY_EDITOR
    [Serializable]
    public class RealmEditorSettings {
        private static string ProjectSettingsDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "ProjectSettings", "com.realmai.unity");
        private static string UserSettingsDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "UserSettings", "com.realmai.unity");
        private static string ProjectSettingsPath => Path.Combine(ProjectSettingsDirectory, "RealmAI.json");
        private static string UserSettingsPath => Path.Combine(UserSettingsDirectory, "RealmAI.json");

        public class ProjectSettings {
            public string PlayerPrefabGuid = "";
        }
        
        public class UserSettings {
            public string EnvSetupCommand = "";
            public string CurrentResultsDirectory = "";
        }
        

        // TODO these can maybe use SettingsProvider instead
        public static ProjectSettings LoadProjectSettings() {
            try {
                if (File.Exists(ProjectSettingsPath)) {
                    var settings = new ProjectSettings();
                    var json = File.ReadAllText(ProjectSettingsPath);
                    EditorJsonUtility.FromJsonOverwrite(json, settings);
                    return settings;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }

            return new ProjectSettings();
        }

        public static void SaveProjectSettings(ProjectSettings settings) {
            if (settings == null)
                settings = new ProjectSettings();
            
            try {
                Directory.CreateDirectory(ProjectSettingsDirectory);
                var json = EditorJsonUtility.ToJson(settings);
                File.WriteAllText(ProjectSettingsPath, json);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
        
        public static UserSettings LoadUserSettings() {
            try {
                if (File.Exists(UserSettingsPath)) {
                    var settings = new UserSettings();
                    var json = File.ReadAllText(UserSettingsPath);
                    EditorJsonUtility.FromJsonOverwrite(json, settings);
                    return settings;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }

            return new UserSettings();
        }

        public static void SaveUserSettings(UserSettings settings) {
            if (settings == null)
                settings = new UserSettings();
            
            try {
                Directory.CreateDirectory(UserSettingsDirectory);
                var json = EditorJsonUtility.ToJson(settings);
                File.WriteAllText(UserSettingsPath, json);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
#endif
}
