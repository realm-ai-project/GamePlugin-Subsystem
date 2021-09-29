using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
    public class RealmSensorComponent : SensorComponent {
        [SerializeField] private RealmOwl _realmOwl = default;
        [SerializeField] private Vector2Delegate _positionFunction = default;

        public override ISensor[] CreateSensors()
        {
            return new ISensor[] { new RealmSensor(_realmOwl, _positionFunction) };
        }
    }

    public class RealmSensor : ISensor
    {
        private RealmOwl _realmOwl = default;
        private Vector2Delegate _positionFunction = default;
        private List<float> _observations = new List<float>();
        
        public RealmSensor(RealmOwl realmOwl, Vector2Delegate positionFunction) {
            _realmOwl = realmOwl;
            _positionFunction = positionFunction;
        }

        public ObservationSpec GetObservationSpec() {
            return ObservationSpec.Vector(2);
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
            var position = _positionFunction.Invoke();
            _observations.Add(position.x);
            _observations.Add(position.y);
            _realmOwl.RecordPosition(position);
        }

        public void Reset() {
        }

        public string GetName() {
            return "RealmSensor";
        }
    }
}