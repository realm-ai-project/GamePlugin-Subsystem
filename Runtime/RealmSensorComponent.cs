using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
    public class RealmSensorComponent : SensorComponent {
        [SerializeField] private RealmOwl _realmOwl = default;
        [SerializeField] private SerializedVector2Func _positionFunction = default;
        [SerializeField] private float _positionRecordInterval = 0.5f;

        private float _flushCooldown = 0;
        
        public override ISensor[] CreateSensors()
        {
            return new ISensor[] { new RealmSensor(_realmOwl, _positionFunction) };
        }
        
        public void StartNewEpisode() {
            _flushCooldown = 0;
        }

        private void Update() {
            _flushCooldown -= Time.deltaTime;
            if (_flushCooldown <= 0) {
                var position = _positionFunction.Invoke();
                _realmOwl.RecordPosition(position);
                _flushCooldown = _positionRecordInterval;
            }
        }
    }

    public class RealmSensor : ISensor
    {
        private RealmOwl _realmOwl = default;
        private SerializedVector2Func _positionFunction = default;
        private List<float> _observations = new List<float>();
        
        public RealmSensor(RealmOwl realmOwl, SerializedVector2Func positionFunction) {
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
        }

        public void Reset() {
        }

        public string GetName() {
            return "RealmSensor";
        }
    }
}