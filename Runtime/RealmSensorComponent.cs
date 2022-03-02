using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEngine;

namespace RealmAI {
    public class RealmSensorComponent : SensorComponent {
        [SerializeField] private RealmOwl _realmOwl = default;
        [SerializeField] private SerializedVector2Func _positionFunction = default;
        [SerializeField] private Rect _approximateMapBounds = new Rect(0, 0, 1, 1);
        [SerializeField] private RealmSensorSpec[] _customSensors = null;
        [SerializeField] private float _positionRecordInterval = 0.5f;
        [SerializeField] private bool _showGizmos = false;
        
        public Rect ApproximateMapBounds {
            get => _approximateMapBounds;
            set => _approximateMapBounds = value;
        }

        public bool ShowGizmos => _showGizmos;
        
        private float _flushCooldown = 0;
        
        public override ISensor[] CreateSensors() {
            return new ISensor[] { new RealmSensor(_positionFunction, _approximateMapBounds, _customSensors) };
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
        public List<float> Observations => _observations;
        public RealmSensorSpec[] CustomSensors => _customSensors;
        
        private SerializedVector2Func _positionFunction = default;
        private RealmSensorSpec[] _customSensors = null;
        private Vector2 _mapMin = default;
        private Vector2 _mapMax = default;
        private bool _normalizePos = false;
        
        private List<float> _observations = new List<float>();
        private ObservationSpec _observationSpec = default;

        public RealmSensor(SerializedVector2Func positionFunction, Rect approximateMapBounds, RealmSensorSpec[] customSensors) {
            _positionFunction = positionFunction;
            _customSensors = customSensors;
            
            // approximate map bounds
            var rect = approximateMapBounds;
            _mapMin = new Vector2(Mathf.Min(rect.x, rect.x + rect.width), Mathf.Min(rect.y, rect.y + rect.height));
            _mapMax = new Vector2(Mathf.Max(rect.x, rect.x + rect.width), Mathf.Max(rect.y, rect.y + rect.height));
            if (Mathf.Abs(rect.width) <= 1e-5 || Mathf.Abs(rect.height) <= 1e-5) {
                _normalizePos = true;
            }
            
            // observation spec
            var observationSize = 2;
            foreach (var sensor in customSensors) {
                switch (sensor.DataType) {
                    case SensorDataType.Bool:
                    case SensorDataType.Int:
                    case SensorDataType.Float:
                        observationSize++;
                        break;
                    case SensorDataType.Vector2:
                        observationSize += 2;
                        break;
                    case SensorDataType.Vector3:
                        observationSize += 3;
                        break;
                }
            }

            _observationSpec = ObservationSpec.Vector(observationSize);
        }

        public ObservationSpec GetObservationSpec() {
            return _observationSpec;
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
            
            // position
            var position = _positionFunction.Invoke();
            if (_normalizePos) {
                position = (position - _mapMin) / (_mapMax - _mapMin);
            }

            _observations.Add(position.x);
            _observations.Add(position.y);
            
            // custom data 
            // TODO error checking and validate int max(?)
            foreach (var sensor in _customSensors) {
                switch (sensor.DataType) {
                    case SensorDataType.Bool:
                        _observations.Add((sensor.BoolFunc?.Invoke() ?? false) ? 1 : 0);
                        break;
                    case SensorDataType.Int:
                        var intVal = (float)Mathf.Clamp(sensor.IntFunc?.Invoke() ?? 0, sensor.IntMin, sensor.IntMaxExclusive);
                        intVal = (intVal - sensor.IntMin)/(sensor.IntMaxExclusive - sensor.IntMin); 
                         _observations.Add(intVal);
                        break;
                    case SensorDataType.Float:
                        _observations.Add(sensor.FloatFunc?.Invoke() ?? 0);
                        break;
                    case SensorDataType.Vector2:
                        var v2 = sensor.Vector2Func?.Invoke() ?? Vector2.zero;
                        _observations.Add(v2.x);
                        _observations.Add(v2.y);
                        break;
                    case SensorDataType.Vector3:
                        var v3 = sensor.Vector3Func?.Invoke() ?? Vector3.zero;
                        _observations.Add(v3.x);
                        _observations.Add(v3.y);
                        _observations.Add(v3.z);
                        break;
                }
            }
        }

        public void Reset() {
        }

        public string GetName() {
            return "RealmSensor";
        }
    }
}