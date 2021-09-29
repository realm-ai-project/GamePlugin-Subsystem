using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;


namespace RealmAI {
    public class SerializedDelegateDrawer<T> : PropertyDrawer {
        private const string TargetName = "_target";
        private const string ScriptName = "_script";
        private const string MethodName = "_methodName";

        private Dictionary<string, int> _typeNameCount = new Dictionary<string, int>();
        
        // TODO a tooltip would be nice?
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, label);

            var targetProp = property.FindPropertyRelative(TargetName);
            var scriptProp = property.FindPropertyRelative(ScriptName);
            var methodProp = property.FindPropertyRelative(MethodName);
            
            // calculate rects           
            var targetRect = position;
            targetRect.width = Mathf.Min(120, position.width/2);

            var methodRect = position;
            methodRect.x = position.x + targetRect.width;
            methodRect.width = position.width - targetRect.width;

            // create property labels
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(targetRect, targetProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck()) {
                scriptProp.objectReferenceValue = null;
                methodProp.stringValue = "";
            }

            // selected method dropdown 
            string selectedMethodLabel;
            if (scriptProp.objectReferenceValue != null) {
                selectedMethodLabel = $"{scriptProp.objectReferenceValue.GetType().Name}.{methodProp.stringValue}";
            } else {
                selectedMethodLabel = "No Method Selected";
            }

            // create method dropdown, which is disabled if no target object is selected yet
            using (new EditorGUI.DisabledScope(targetProp.objectReferenceValue == null)) {
                if (EditorGUI.DropdownButton(methodRect, new GUIContent(selectedMethodLabel), FocusType.Passive)) {
                    if (targetProp.objectReferenceValue is GameObject target) {
                        // create dropdown
                        var menu = new GenericMenu();

                        menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(methodProp.stringValue), OnSelected, (property, target));
                        menu.AddSeparator("");

                        // find attached scripts on game object
                        var attachedScripts = target.GetComponents<MonoBehaviour>();

                        // count types for duplicate type name checks
                        // if there are duplicate type names, we need to display the namespace for those types to differentiate them
                        _typeNameCount.Clear();
                        foreach (var script in attachedScripts) {
                            var type = script.GetType();
                            if (_typeNameCount.TryGetValue(type.Name, out var count)) {
                                _typeNameCount[type.Name] = count + 1;
                            } else {
                                _typeNameCount.Add(type.Name, 1);
                            }
                        }

                        foreach (var script in attachedScripts) {
                            if (script == null)
                                continue;

                            var type = script.GetType();

                            // if there multiple types with the same name, show their namespace when displayed
                            string typePath;
                            _typeNameCount.TryGetValue(type.Name, out var count);
                            if (count == 1) {
                                typePath = type.Name;
                            } else if (string.IsNullOrEmpty(type.Namespace)) {
                                typePath = type.Name;
                            } else {
                                typePath = $"{type.Namespace}.{type.Name}";
                            }

                            // filter for acceptable methods on script
                            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var method in methods) {
                                if (method.GetParameters().Length != 0 || method.ReturnType != typeof(T))
                                    continue;

                                var selected = scriptProp.objectReferenceValue == script && methodProp.stringValue == method.Name;
                                menu.AddItem(new GUIContent($"{typePath}/{method.Name}"), selected, OnSelected, (property, target, script, method));
                            }
                        }

                        menu.ShowAsContext();

                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private void OnSelected(object o) {
            // this will be called from the context menu and not during the OnGUI code
            // so we need to wrap update/apply to see new changes/preserve our changes
            {
                if (o is (SerializedProperty property, GameObject target)) {
                    property.serializedObject.Update();
                    property.FindPropertyRelative(TargetName).objectReferenceValue = target;
                    property.FindPropertyRelative(ScriptName).objectReferenceValue = null;
                    property.FindPropertyRelative(MethodName).stringValue = "";
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            {
                if (o is (SerializedProperty property, GameObject target, MonoBehaviour script, MethodInfo method)) {
                    property.serializedObject.Update();
                    property.FindPropertyRelative(TargetName).objectReferenceValue = target;
                    property.FindPropertyRelative(ScriptName).objectReferenceValue = script;
                    property.FindPropertyRelative(MethodName).stringValue = method.Name;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(FloatDelegate))]
    public class FloatDelegateDrawer : SerializedDelegateDrawer<float> { }

    [CustomPropertyDrawer(typeof(IntDelegate))]
    public class IntDelegateDrawer : SerializedDelegateDrawer<int> { }
    
    [CustomPropertyDrawer(typeof(Vector2Delegate))]
    public class Vector2DelegateDrawer : SerializedDelegateDrawer<Vector2> { }
    
    [CustomPropertyDrawer(typeof(BoolDelegate))]
    public class Bool2DelegateDrawer : SerializedDelegateDrawer<bool> { }
}