using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.PlayerLoop;


namespace RealmAI {
    public class SerializedDelegate<T> {
        [SerializeField] protected GameObject _target;
        [SerializeField] protected  UnityEngine.Object _script;
        [SerializeField] protected string _methodName;

        public GameObject Target {
            get => _target;
            set {
                _target = value;
                UpdateCachedMethod();
            }
        }
        
        public UnityEngine.Object Script{
            get => _script;
            set {
                _script = value;
                UpdateCachedMethod();
            }
        }
        
        public string MethodName{
            get => _methodName;
            set {
                _methodName = value;
                UpdateCachedMethod();
            }
        }
        
        private MethodInfo _cachedMethod = null;
        
        public void UpdateCachedMethod() {
            // TODO does this work with obfuscated code?
            if (Script == null || string.IsNullOrEmpty(MethodName)) {
                // TODO Warning?
                return;
            }

            _cachedMethod = Script.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance, null, new Type[0], new ParameterModifier[0]);
        }


        public T Invoke() {
            if (_cachedMethod == null) {
                if (Script == null || string.IsNullOrEmpty(MethodName)) {
                    // TODO Warning?
                    return default;
                }

                UpdateCachedMethod();
            }

            if (_cachedMethod == null) {
                // TODO Error
                return default;
            }

            var returnVal = _cachedMethod.Invoke(Script, null);
            if (returnVal is T val) {
                return val;
            }
            
            // TODO Error
            return default;
        }
    }
    
    [Serializable]
    public class FloatDelegate : SerializedDelegate<float>{ }
    
    [Serializable]
    public class IntDelegate : SerializedDelegate<int>{ }
    
    [Serializable]
    public class Vector2Delegate : SerializedDelegate<Vector2>{ }
    
    [Serializable]
    public class BoolDelegate : SerializedDelegate<bool>{ }
}
