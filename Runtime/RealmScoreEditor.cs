using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace RealmAI {
    [CustomEditor(typeof(RealmScore))]
    public class RealmScoreEditor : Editor {

        private void OnSceneGUI() {
            var t = target as RealmScore;

            if (t == null || t.RewardRegions == null)
                return;

            var c = Handles.color;
            var g = GUI.color;

            if (Application.isPlaying) {
                var scoreLabel = $"score: {t.GetScore()}";
                Handles.Label(t.transform.position, scoreLabel, EditorStyles.boldLabel);
            }

            for (int i = 0; i < t.RewardRegions.Length; i++) {
                var region = t.RewardRegions[i];
                if (!region.Enabled)
                    continue;



                Handles.color = region.DebugColor;
                GUI.color = region.DebugColor;
                for (int j = 0; j < region.Rects.Length; j++) {
                    var rect = region.Rects[j];

                    if (j == 0) {
                        var label = "";
                        if (Mathf.Abs(region.EnterReward) > 1e-6f) {
                            label += region.EnterReward > 0 ? "+" : "";

                            if (Mathf.Abs(region.StayRewardPerSecond) > 1e-6f) {
                                label += region.EnterReward + ", ";
                            }
                        }

                        if (Mathf.Abs(region.StayRewardPerSecond) > 1e-6f) {
                            label += region.StayRewardPerSecond > 0 ? "+" : "";
                            label += region.StayRewardPerSecond + "/s";
                        }

                        Handles.Label(new Vector3(Mathf.Min(rect.xMin, rect.xMax) + 2f, Mathf.Max(rect.yMin, rect.yMax) - 2f), label, EditorStyles.boldLabel);
                    }

                    Handles.DrawWireCube(rect.center, rect.size);
                    var handleSize = HandleUtility.GetHandleSize(rect.center) * 0.2f;
                    var handleSnap = Vector3.one * 0.5f;

                    Handles.color = region.DebugColor;
                    EditorGUI.BeginChangeCheck();
                    var handle1Pos = Handles.FreeMoveHandle(rect.center - rect.size / 2f, Quaternion.identity, handleSize, handleSnap, Handles.SphereHandleCap);
                    var handle2Pos = Handles.FreeMoveHandle(rect.center + rect.size / 2f, Quaternion.identity, handleSize, handleSnap, Handles.SphereHandleCap);

                    if (EditorGUI.EndChangeCheck()) {
                        Undo.RecordObject(t, "Change RewardRegion Rect");
                        region.Rects[j] = Rect.MinMaxRect(handle1Pos.x, handle1Pos.y, handle2Pos.x, handle2Pos.y);
                    }
                }
            }

            Handles.color = c;
            GUI.color = g;
        }

        private Vector3[] GetVertices(Rect rect) {
            return new[] {
                new Vector3(rect.x, rect.y, 0f),
                new Vector3(rect.x + rect.width, rect.y, 0f),
                new Vector3(rect.x + rect.width, rect.y + rect.height, 0f),
                new Vector3(rect.x, rect.y + rect.height, 0f),
            };
        }
    }
}