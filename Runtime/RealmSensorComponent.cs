using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
    public class RealmSensorComponent : SensorComponent {
        [SerializeField] private FloatDelegate _rewardFunction = default;
        [SerializeField] private IntDelegate _test = default;

        public void Awake() {
            print(_rewardFunction.Invoke());
        }

        public override ISensor[] CreateSensors()
        {
            return new ISensor[] { new RealmSensor(_rewardFunction) };
        }
    }

    public class RealmSensor : ISensor
    {
        private FloatDelegate _rewardFunction = default;
        private List<float> _observations = new List<float>();
        
        public RealmSensor(FloatDelegate rewardFunction) {
            _rewardFunction = rewardFunction;
        }

        public ObservationSpec GetObservationSpec() {
            return ObservationSpec.Vector(1);
        }

        public CompressionSpec GetCompressionSpec() {
            return CompressionSpec.Default();
        }
        
        public int Write(ObservationWriter writer) {
            writer.AddList(_observations);
            return _observations.Count;
        }

        public byte[] GetCompressedObservation() {
            return null;
        }

        public void Update() {
            _observations.Clear();
            var reward = _rewardFunction.Invoke();
            _observations.Add(reward);
        }

        public void Reset() {
        }

        public string GetName() {
            return "RealmSensor";
        }
    }
}