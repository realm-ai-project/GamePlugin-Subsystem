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

        public float Min = 0;
        public float Max = 1;
        public UnityEvent<float> Callback = default;
    }

    [Serializable]
    public class RealmDiscreteActionSpec {
        public delegate int DiscreteHeuristicDelegate();

        public int Branches = 2;
        public UnityEvent<int> Callback = default;
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
                var val = spec.Min + actionBuffers.ContinuousActions[i] * (spec.Max - spec.Min);
                _continuousActionSpecs[i].Callback?.Invoke(val);
            }

            for (int i = 0; i < _discreteActionSpecs.Length; i++) {
                _discreteActionSpecs[i].Callback?.Invoke(actionBuffers.DiscreteActions[i]);
            }
        }

        public void Heuristic(in ActionBuffers actionBuffersOut) {
            // TODO
        }

        public void WriteDiscreteActionMask(IDiscreteActionMask actionMask) {

        }
    }
}