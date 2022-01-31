using System;
using System.Reflection;
using UnityEngine;


namespace RealmAI {
    public abstract class SerializedDelegate {
        [SerializeField] protected GameObject _target;
        [SerializeField] protected UnityEngine.Object _script;
        [SerializeField] protected string _methodName;

        public GameObject Target {
            get => _target;
            set {
                _target = value;
                _cachedMethod = GetMethodInfo();
            }
        }

        public UnityEngine.Object Script {
            get => _script;
            set {
                _script = value;
                _cachedMethod = GetMethodInfo();
            }
        }

        public string MethodName {
            get => _methodName;
            set {
                _methodName = value;
                _cachedMethod = GetMethodInfo();
            }
        }

        protected MethodInfo MethodInfo {
            get {
                if (_cachedMethod == null) {
                    if (Script == null || string.IsNullOrEmpty(MethodName)) {
                        // TODO Warning?
                        return null;
                    }

                    _cachedMethod = GetMethodInfo();
                }

                return _cachedMethod;
            }
        }

        private MethodInfo _cachedMethod = null;

        protected virtual MethodInfo GetMethodInfo() {
            // TODO does this work with obfuscated code?
            if (Script == null || string.IsNullOrEmpty(MethodName)) {
                // TODO Warning?
                return null;
            }

            return Script.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance, null, new Type[0], new ParameterModifier[0]);
        }
    }
    
    [Serializable]
    public class SerializedAction : SerializedDelegate{
        public virtual void Invoke() {
            if (MethodInfo == null) {
                // TODO Error
                return;
            }

            MethodInfo.Invoke(Script, null);
        }
    }
    
    public class SerializedAction<T> : SerializedDelegate{
        protected override MethodInfo GetMethodInfo() {
            // TODO does this work with obfuscated code?
            if (Script == null || string.IsNullOrEmpty(MethodName)) {
                // TODO Warning?
                return null;
            }

            return Script.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance, null, new Type[] {typeof(T)}, new ParameterModifier[0]);
        }
        
        
        public void Invoke(T arg) {
            if (MethodInfo == null) {
                // TODO Error
                return ;
            }

            MethodInfo.Invoke(Script, new object[] {arg});
        }
    }
    
    public class SerializedFunc<TResult> : SerializedDelegate {
        public virtual TResult Invoke() {
            if (MethodInfo == null) {
                // TODO Error
                return default;
            }

            var returnVal = MethodInfo.Invoke(Script, null);
            if (returnVal is TResult val) {
                return val;
            }

            // TODO Error
            return default;
        }
    }

    public class SerializedFunc<T, TResult> : SerializedDelegate {
        protected override MethodInfo GetMethodInfo() {
            // TODO does this work with obfuscated code?
            if (Script == null || string.IsNullOrEmpty(MethodName)) {
                // TODO Warning?
                return null;
            }

            return Script.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance, null, new Type[] {typeof(T)}, new ParameterModifier[0]);
        }


        public TResult Invoke(T arg) {
            if (MethodInfo == null) {
                // TODO Error
                return default;
            }

            var returnVal = MethodInfo.Invoke(Script, new object[] {arg});
            if (returnVal is TResult val) {
                return val;
            }

            // TODO Error
            return default;
        }
    }
    
    
    [Serializable]
    public class SerializedFloatAction : SerializedAction<float> {
    }
    
    [Serializable]
    public class SerializedIntAction : SerializedAction<int> {
    }
    
    [Serializable]
    public class SerializedBoolAction : SerializedAction<bool> {
    }
    
    [Serializable]
    public class SerializedVector2Action : SerializedAction<Vector2> {
    }
    
    [Serializable]
    public class SerializedVector3Action : SerializedAction<Vector3> {
    }

    
    [Serializable]
    public class SerializedFloatFunc : SerializedFunc<float> {
    }

    [Serializable]
    public class SerializedIntFunc : SerializedFunc<int> {
    }

    [Serializable]
    public class SerializedBoolFunc : SerializedFunc<bool> {
    }
    
    [Serializable]
    public class SerializedVector2Func : SerializedFunc<Vector2> {
    }
    
    [Serializable]
    public class SerializedVector3Func : SerializedFunc<Vector3> {
    }

    [Serializable]
    public class SerializedBoolIntFunc : SerializedFunc<bool, int> {
    }
}
