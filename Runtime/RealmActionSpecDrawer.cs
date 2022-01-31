using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RealmAI {
    [CustomPropertyDrawer(typeof(RealmActionSpec))]
    public class RealmActionSpecDrawer : PropertyDrawer {
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
            var dataType = (ActionDataType) dataTypeProp.enumValueIndex;

            EditorGUI.indentLevel++;
            position = PropertyField(position, property, "DataType", "Data Type");
            switch (dataType) {
                case ActionDataType.Bool:
                    position = PropertyField(position, property, "BoolCallback", "Callback");
                    position = PropertyField(position, property, "BoolHeuristic", "Heuristic");
                    break;
                case ActionDataType.Int:
                    position = PropertyField(position, property, "IntCallback", "Callback");
                    position = PropertyField(position, property, "IntHeuristic", "Heuristic");
                    position = PropertyField(position, property, "IntMaxExclusive", "Max Value (Exclusive)");
                    break;
                case ActionDataType.Float:
                    position = PropertyField(position, property, "FloatCallback", "Callback");
                    position = PropertyField(position, property, "FloatHeuristic", "Heuristic");
                    break;
                case ActionDataType.Vector2:
                    position = PropertyField(position, property, "Vector2Callback", "Callback");
                    position = PropertyField(position, property, "Vector2Heuristic", "Heuristic");
                    break;
                case ActionDataType.Vector3:
                    position = PropertyField(position, property, "Vector3Callback", "Callback");
                    position = PropertyField(position, property, "Vector3Heuristic", "Heuristic");
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
            var height = EditorStyles.boldLabel.CalcSize(label).y + EditorGUI.GetPropertyHeight(dataTypeProp);
            height += 2 * EditorGUIUtility.standardVerticalSpacing;
            switch (dataType) {
                case ActionDataType.Bool:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("BoolCallback"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("BoolHeuristic"));
                    height += 2 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Int:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IntCallback"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IntHeuristic"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IntMaxExclusive"));
                    height += 3 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Float:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("FloatCallback"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("FloatHeuristic"));
                    height += 2 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Vector2:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vector2Callback"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vector2Heuristic"));
                    height += 2 * EditorGUIUtility.standardVerticalSpacing;
                    break;
                case ActionDataType.Vector3:
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vector3Callback"));
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vector3Heuristic"));
                    height += 2 * EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            return height;
        }
    }
}