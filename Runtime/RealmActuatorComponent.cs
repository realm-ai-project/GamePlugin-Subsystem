using System;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using UnityEngine;

namespace RealmAI {
    public class RealmActuatorComponent : ActuatorComponent {
        
        [SerializeField] private RealmActionSpec[] _actions = null;
        private ActionSpec _actionSpec = default;
        private bool _actionSpecInitialized = false;

        public override IActuator[] CreateActuators() {
            return new IActuator[] {new RealmActuator(_actions)};
        }

        public override ActionSpec ActionSpec {
            get {
                if (!_actionSpecInitialized) {
                    _actionSpec = MakeActionSpec(_actions);
                    _actionSpecInitialized = true;
                }

                return _actionSpec;
            }
        }

        internal static ActionSpec MakeActionSpec(RealmActionSpec[] actions) {
            var continuousActions = 0;
            var discreteActions = new List<int>();
            foreach (var action in actions) {
                switch (action.DataType) {
                    case ActionDataType.Bool:
                        discreteActions.Add(2);
                        break;
                    case ActionDataType.Int:
                        discreteActions.Add(Mathf.Max(2, action.IntMaxExclusive));
                        break;
                    case ActionDataType.Float:
                        continuousActions++;
                        break;
                    case ActionDataType.Vector2:
                        continuousActions += 2;
                        break;
                    case ActionDataType.Vector3:
                        continuousActions += 3;
                        break;
                }
            }

            return new ActionSpec(continuousActions, discreteActions.ToArray());
        }
    }

    public class RealmActuator : IActuator {
        private RealmActionSpec[] _actions = null;
        private ActionSpec _actionSpec = default;

        public RealmActuator(RealmActionSpec[] actions) {
            _actions = actions;
            _actionSpec = RealmActuatorComponent.MakeActionSpec(_actions);
        }

        public ActionSpec ActionSpec => _actionSpec;
        public String Name => "RealmActuator";
        
        public void ResetData() {

        }

        public void OnActionReceived(ActionBuffers actionBuffers) {
            var continuousActionIndex = 0;
            var discreteActionIndex = 0;
            foreach (var action in _actions) {
                switch (action.DataType) {
                    case ActionDataType.Bool:
                        action.BoolCallback?.Invoke(actionBuffers.DiscreteActions[discreteActionIndex] == 1);
                        discreteActionIndex++;
                        break;
                    case ActionDataType.Int:
                        action.IntCallback?.Invoke(actionBuffers.DiscreteActions[discreteActionIndex]);
                        discreteActionIndex++;
                        break;
                    case ActionDataType.Float:
                        action.FloatCallback?.Invoke(actionBuffers.ContinuousActions[continuousActionIndex]);
                        continuousActionIndex++;
                        break;
                    case ActionDataType.Vector2:
                        var v2 = new Vector2(actionBuffers.ContinuousActions[continuousActionIndex],
                            actionBuffers.ContinuousActions[continuousActionIndex + 1]);
                        action.Vector2Callback?.Invoke(v2);
                        continuousActionIndex += 2;
                        break;
                    case ActionDataType.Vector3:
                        var v3 = new Vector3(actionBuffers.ContinuousActions[continuousActionIndex],
                            actionBuffers.ContinuousActions[continuousActionIndex + 1],
                            actionBuffers.ContinuousActions[continuousActionIndex + 2]);
                        action.Vector3Callback?.Invoke(v3);
                        continuousActionIndex += 3;
                        break;
                }
            }
        }

        public void Heuristic(in ActionBuffers actionBuffersOut) {
            var continuousActions = actionBuffersOut.ContinuousActions;
            var discreteActions = actionBuffersOut.DiscreteActions;
            
            var continuousActionIndex = 0;
            var discreteActionIndex = 0;
            // TODO error checking and validate int max 
            foreach (var action in _actions) {
                switch (action.DataType) {
                    case ActionDataType.Bool:
                        discreteActions[discreteActionIndex] = (action.BoolHeuristic?.Invoke() ?? false) ? 1 : 0;
                        discreteActionIndex++;
                        break;
                    case ActionDataType.Int:
                        discreteActions[discreteActionIndex] = Mathf.Clamp(action.IntHeuristic?.Invoke() ?? 0, 0, action.IntMaxExclusive);
                        discreteActionIndex++;
                        break;
                    case ActionDataType.Float:
                        continuousActions[continuousActionIndex] = action.FloatHeuristic?.Invoke() ?? 0;
                        continuousActionIndex++;
                        break;
                    case ActionDataType.Vector2:
                        var v2 = action.Vector2Heuristic?.Invoke() ?? Vector2.zero;
                        continuousActions[continuousActionIndex] = v2.x;
                        continuousActions[continuousActionIndex + 1] = v2.y;
                        continuousActionIndex += 2;
                        break;
                    case ActionDataType.Vector3:
                        var v3 = action.Vector3Heuristic?.Invoke() ?? Vector3.zero;
                        continuousActions[continuousActionIndex] = v3.x;
                        continuousActions[continuousActionIndex + 1] = v3.y;
                        continuousActions[continuousActionIndex + 2] = v3.y;
                        continuousActionIndex += 3;
                        break;
                }
            }
        }

        public void WriteDiscreteActionMask(IDiscreteActionMask actionMask) {

        }
    }
}