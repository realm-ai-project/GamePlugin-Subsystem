using System;
using System.Linq;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.Events;

namespace RealmAI {

    // TODO ADD EDITOR SCRIPT + validations
    [Serializable]
    public class RealmContinuousActionSpec {
        public delegate float ContinuousHeuristicDelegate();
        // TODO can also make presets like joysticks/key presses, things that can drop-in and replace common inputs
        public UnityEvent<float> Callback = default;
        public FloatDelegate Heuristic = default;
    }

    [Serializable]
    public class RealmDiscreteActionSpec {
        public delegate int DiscreteHeuristicDelegate();

        public int Branches = 2;
        public UnityEvent<int> Callback = default; //TODO we can also do one callback per branch
        public IntDelegate Heuristic = default;
    }

    public class RealmActuatorComponent : ActuatorComponent {
        [SerializeField] private RealmContinuousActionSpec[] _continuousActionSpecs = null;
        [SerializeField] private RealmDiscreteActionSpec[] _discreteActionSpecs = null;
        private ActionSpec _actionSpec = default;
        private bool _actionSpecInitialized = false;

        public override IActuator[] CreateActuators() {
            return new IActuator[] {new RealmActuator(_continuousActionSpecs, _discreteActionSpecs)};
        }

        public override ActionSpec ActionSpec {
            get {
                if (!_actionSpecInitialized) {
                    _actionSpec = MakeActionSpec(_continuousActionSpecs, _discreteActionSpecs);
                    _actionSpecInitialized = true;
                }

                return _actionSpec;
            }
        }

        internal static ActionSpec MakeActionSpec(RealmContinuousActionSpec[] continuousActionSpecs, RealmDiscreteActionSpec[] discreteActionSpecs) {
            return new ActionSpec(continuousActionSpecs.Length, discreteActionSpecs.Select(x => x.Branches).ToArray());
        }
    }

    public class RealmActuator : IActuator {
        [SerializeField] private RealmContinuousActionSpec[] _continuousActionSpecs = null;
        [SerializeField] private RealmDiscreteActionSpec[] _discreteActionSpecs = null;
        private ActionSpec _actionSpec = default;

        public RealmActuator(RealmContinuousActionSpec[] continuousActionSpecs, RealmDiscreteActionSpec[] discreteActionSpecs) {
            _continuousActionSpecs = continuousActionSpecs;
            _discreteActionSpecs = discreteActionSpecs;
            _actionSpec = RealmActuatorComponent.MakeActionSpec(continuousActionSpecs, discreteActionSpecs);
        }

        public ActionSpec ActionSpec {
            get { return _actionSpec; }
        }

        public String Name {
            get { return "RealmActuator"; }
        }

        public void ResetData() {

        }

        public void OnActionReceived(ActionBuffers actionBuffers) {
            for (int i = 0; i < _continuousActionSpecs.Length; i++) {
                var spec = _continuousActionSpecs[i];
                _continuousActionSpecs[i].Callback?.Invoke(actionBuffers.ContinuousActions[i]);
            }

            for (int i = 0; i < _discreteActionSpecs.Length; i++) {
                _discreteActionSpecs[i].Callback?.Invoke(actionBuffers.DiscreteActions[i]);
            }
        }

        public void Heuristic(in ActionBuffers actionBuffersOut) {
            var continuousActions = actionBuffersOut.ContinuousActions;
            var discreteActions = actionBuffersOut.DiscreteActions;
            
            for (int i = 0; i < _continuousActionSpecs.Length; i++) {
                // TODO Error checking?
                if (_continuousActionSpecs[i].Heuristic != null) {
                    var spec = _continuousActionSpecs[i];
                    var val = _continuousActionSpecs[i].Heuristic.Invoke();
                    continuousActions[i] = val;
                } else {
                    continuousActions[i] = 0;
                }
            }

            for (int i = 0; i < _discreteActionSpecs.Length; i++) {
                // TODO Error checking?
                if (_discreteActionSpecs[i].Heuristic != null) {
                    discreteActions[i] = _discreteActionSpecs[i].Heuristic.Invoke();
                } else {
                    discreteActions[i] = 0;
                }
            }
        }

        public void WriteDiscreteActionMask(IDiscreteActionMask actionMask) {

        }
    }
}