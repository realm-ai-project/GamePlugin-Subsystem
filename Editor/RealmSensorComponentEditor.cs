using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace RealmAI {
    [CustomEditor(typeof(RealmSensorComponent))]
    public class RealmSensorComponentEditor : Editor {

        private void OnSceneGUI() {
            var t = target as RealmSensorComponent;

            if (t == null || !t.ShowGizmos)
                return;

            var c = Handles.color;
            var g = GUI.color;

            Handles.color = Color.black;
            GUI.color = Color.black;

            var rect = t.ApproximateMapBounds;
            var label = "Approximate Map Bounds";
            Handles.Label(new Vector3(Mathf.Min(rect.xMin, rect.xMax) + 0.1f, Mathf.Max(rect.yMin, rect.yMax) - 0.1f), label, EditorStyles.boldLabel);
            
            Handles.DrawWireCube(rect.center, rect.size);
            var handleSize = HandleUtility.GetHandleSize(rect.center) * 0.2f;
            var handleSnap = Vector3.one * 0.5f;

            EditorGUI.BeginChangeCheck();
            var handle1Pos = Handles.FreeMoveHandle(rect.center - rect.size / 2f, Quaternion.identity, handleSize, handleSnap, Handles.SphereHandleCap);
            var handle2Pos = Handles.FreeMoveHandle(rect.center + rect.size / 2f, Quaternion.identity, handleSize, handleSnap, Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(t, "Change RealmAI Approximate Map Bounds");
                t.ApproximateMapBounds = Rect.MinMaxRect(handle1Pos.x, handle1Pos.y, handle2Pos.x, handle2Pos.y);
            }

            
            // debug info
            var debugSensor = (RealmSensor)t.CreateSensors().FirstOrDefault();
            if (debugSensor != null) {
                debugSensor.Update();
                var sb = new StringBuilder();
                sb.AppendLine("Sensors:");
                
                sb.Append("  Position: ");
                var pos = sb.Append(new Vector2(debugSensor.Observations[0], debugSensor.Observations[1]));
                sb.AppendLine();
                
                var i = 2;
                foreach (var sensor in debugSensor.CustomSensors) {
                    sb.Append("  ");
                    sb.Append(sensor.Label);
                    sb.Append(": ");
                    switch (sensor.DataType) {
                        case SensorDataType.Bool:
                            sb.Append(debugSensor.Observations[i]);
                            i++;
                            break;
                        case SensorDataType.Int:
                            sb.Append(debugSensor.Observations[i]);
                            i++;
                            break;
                        case SensorDataType.Float:
                            sb.Append(debugSensor.Observations[i]);
                            i++;
                            break;
                        case SensorDataType.Vector2:
                            sb.Append(new Vector2(debugSensor.Observations[i], debugSensor.Observations[i + 1]));
                            i += 2;
                            break;
                        case SensorDataType.Vector3:
                            sb.Append(new Vector3(debugSensor.Observations[i], debugSensor.Observations[i + 1], debugSensor.Observations[i + 2]));
                            i += 3;
                            break;
                    }

                    sb.AppendLine();
                }
                
                Handles.Label(t.transform.position, sb.ToString(), EditorStyles.boldLabel);
            }

            Handles.color = c;
            GUI.color = g;
        }
    }
}