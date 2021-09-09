using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
    public class RealmAgent : Agent {
        private List<int> _stuff = new List<int>();
        
        private void Awake() {
            Debug.Log("Hello World!");
        }
    
        public override void OnEpisodeBegin() {
        }
    
        public override void CollectObservations(VectorSensor sensor) {
        }
    
        public override void OnActionReceived(ActionBuffers actions) {
        }
    
        public override void Heuristic(in ActionBuffers actionsOut) {
        }
    }
}