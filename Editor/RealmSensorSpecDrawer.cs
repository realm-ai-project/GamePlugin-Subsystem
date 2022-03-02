using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RealmAI {
    [CustomPropertyDrawer(typeof(RealmSensorSpec))]
    public class RealmSensorSpecDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var labelSize = EditorStyles.boldLabel.CalcSize(label);
            var labelRect = position;
            labelRect.width = labelSize.x;
            labelRect.height = labelSize.y;
            EditorGUI.LabelField(labelRect, label.text, EditorStyles.boldLabel);
            position.y += labelSize.y + EditorGUIUtility.standardVerticalSpacing;
            position.height -= labelSize.y;

            var dataTypeProp = property.FindPropertyRelative("DataType");
            var dataType = (SensorDataType) dataTypeProp.enumValueIndex;

            EditorGUI.indentLevel++;
            position = PropertyField(position, property, "Label", "Label (Debug Only)");
            position = PropertyField(position, property, "DataType", "Data Type");
            switch (dataType) {
                case SensorDataType.Bool:
                    position = PropertyField(position, property, "BoolFunc", "Function");
                    break;
                case SensorDataType.Int:
                    position = PropertyField(position, property, "IntFunc", "Function");
                    position = PropertyField(position, property, "IntMin", "Int Value (Inclusive)");
                    position = PropertyField(position, property, "IntMaxExclusive", "Max Value (Exclusive)");
                    break;
                case SensorDataType.Float:
                    position = PropertyField(position, property, "FloatFunc", "Function");
                    break;
                case SensorDataType.Vector2:
                    position = PropertyField(position, property, "Vector2Func", "Function");
                    break;
                case SensorDataType.Vector3:
                    position = PropertyField(position, property, "Vector3Func", "Function");
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private Rect PropertyField(Rect position, SerializedProperty parentProperty, string propertyName, string label) {
            var prop = parentProperty.FindPropertyRelative(propertyName);
            var rect = position;
            rect.height = EditorGUI.GetPropertyHeight(prop);
            EditorGUI.PropertyField(rect, prop, new GUIContent(label));
            position.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            position.height -= rect.height;
            return position;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var dataTypeProp = property.FindPropertyRelative("DataType");
            var dataType = (ActionDataType) dataTypeProp.enumValueIndex;
            var height = EditorStyles.boldLabel.CalcSize(label).y;
            height += EditorGUI.GetPropertyHeight(dataTypeProp);
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Label"));
            height += 3 * EditorGUIUtility.standardVerticalSpacing;
            switch (dataType) {
                case ActionDataType.Bool:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("BoolFunc"));
                    height += 1 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Int:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IntFunc"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IntMin"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IntMaxExclusive"));
                    height += 3 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Float:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("FloatFunc"));
                    height += 1 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Vector2:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vector2Func"));
                    height += 1 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Vector3:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vector3Func"));
                    height += 1 * EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            return height;
        }
    }
}