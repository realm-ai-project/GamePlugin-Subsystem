using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealmAI {
#if UNITY_EDITOR
    [Serializable]
    public class RealmEditorSettings {
        private static string ConfigStateDirectory => $"{Path.GetDirectoryName(Application.dataPath)}/ProjectSettings/com.realmai.unity";
        private static string ConfigStatePath => $"{ConfigStateDirectory}/RealmAI.json";

        public string PlayerPrefabGuid = "";
        public string CurrentResultsDirectory = "";

        public static RealmEditorSettings LoadSettings() {
            try {
                if (File.Exists(ConfigStatePath)) {
                    var settings = new RealmEditorSettings();
                    var json = File.ReadAllText(ConfigStatePath);
                    EditorJsonUtility.FromJsonOverwrite(json, settings);
                    return settings;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }

            return null;
        }

        public static void SaveSettings(RealmEditorSettings settings) {
            try {
                Directory.CreateDirectory(ConfigStateDirectory);
                var json = EditorJsonUtility.ToJson(settings);
                File.WriteAllText(ConfigStatePath, json);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
#endif
}
