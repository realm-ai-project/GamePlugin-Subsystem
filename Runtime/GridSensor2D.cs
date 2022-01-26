using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
    // simplified but almost directly copied version of Unity.MLAgents GridSensorBase
    // but it works for 2D physics
    public class GridSensor2D : ISensor {
        private Vector2 _cellSize;
        private Vector2Int _cellCount;
        private string[] _detectableTags;
        private GameObject _parentGameObject;
        private LayerMask _colliderMask;
        private int _maxColliderBufferSize;
        private SensorCompressionType _compressionType;

        // Buffers
        private float[] _perceptionBuffer;
        private Color[] _perceptionColors;
        private Texture2D _perceptionTexture;
        private float[] _cellDataBuffer;
        private Collider2D[] _colliderBuffer;
        
        // Utility Constants Calculated on Init
        private int _numCells;
        private int _cellObservationSize;
        private ObservationSpec _observationSpec;
        private Vector2[] _cellLocalPositions;

        public float[] PerceptionBuffer => _perceptionBuffer;
        public Vector2[] CellPositions => _cellLocalPositions;

        protected string[] DetectableTags => _detectableTags;

        public GridSensor2D(
            Vector2 cellSize,
            Vector2Int cellCount,
            string[] detectableTags,
            GameObject parentGameObject,
            LayerMask colliderMask,
            SensorCompressionType compressionType = SensorCompressionType.None,
            int initialColliderBufferSize = 10,
            int maxColliderBufferSize = 500) {

            if (compressionType == SensorCompressionType.PNG && (cellSize.x < 20 || cellSize.y < 20)) {
                Debug.Log("Grid sensor grid size is too small for PNG compression, using no compression.");
                compressionType = SensorCompressionType.None;
            }
            
            _cellSize = cellSize;
            _cellCount = cellCount;
            _detectableTags = detectableTags;
            _parentGameObject = parentGameObject;
            _colliderMask = colliderMask;
            _maxColliderBufferSize = maxColliderBufferSize;
            _compressionType = compressionType;

            _numCells = cellCount.x * cellCount.y;
            _cellObservationSize = GetCellObservationSize();
            _observationSpec = ObservationSpec.Visual(cellCount.y, cellCount.x, _cellObservationSize);
            _perceptionTexture = new Texture2D(cellCount.x, cellCount.x, TextureFormat.RGB24, false);

            _cellLocalPositions = new Vector2[_numCells];
            for (int i = 0; i < cellCount.x; i++) {
                for (int j = 0; j < cellCount.y; j++) {
                    var x = (i - (cellCount.x - 1) / 2f) * cellSize.x;
                    var y = (j - (cellCount.y - 1) / 2f) * cellSize.y;
                    _cellLocalPositions[i + j * cellCount.y] = new Vector2(x, y);
                }
            }

            _colliderBuffer = new Collider2D[Math.Min(initialColliderBufferSize, maxColliderBufferSize)];
            ResetPerceptionBuffer();
        }

        public void ResetPerceptionBuffer()
        {
            if (_perceptionBuffer != null)
            {
                Array.Clear(_perceptionBuffer, 0, _perceptionBuffer.Length);
                Array.Clear(_cellDataBuffer, 0, _cellDataBuffer.Length);
            }
            else
            {
                _perceptionBuffer = new float[_cellObservationSize * _numCells];
                _cellDataBuffer = new float[_cellObservationSize];
                _perceptionColors = new Color[_numCells];
            }
        }

        public ObservationSpec GetObservationSpec() {
            return _observationSpec;
        }

        public CompressionSpec GetCompressionSpec() {
            return new CompressionSpec(_compressionType);
        }

        protected virtual int GetCellObservationSize() {
            return 1;
        }
        
        public void Update() {
            ResetPerceptionBuffer();
            
            // TODO allow rotate with agent
            // TODO observation stacks
            // simplified from Unity.MLAgents.Sensors.BoxOverlapChecker
            for (var cellIndex = 0; cellIndex < _numCells; cellIndex++)
            {
                var cellCenter = _cellLocalPositions[cellIndex] + (Vector2)_parentGameObject.transform.position;
                
                int numFound;
                while (true)
                {
                    numFound = Physics2D.OverlapBoxNonAlloc(cellCenter, _cellSize / 2f, 0, _colliderBuffer, _colliderMask);
                    if (numFound == _colliderBuffer.Length && _colliderBuffer.Length < _maxColliderBufferSize)
                    {
                        _colliderBuffer = new Collider2D[Math.Min(_maxColliderBufferSize, _colliderBuffer.Length * 2)];
                    }
                    else
                    {
                        break;
                    }
                }
                ParseCollidersClosest(_colliderBuffer, numFound, cellIndex, cellCenter);
            }
        }

        void ParseCollidersClosest(Collider2D[] foundColliders, int numFound, int cellIndex, Vector3 cellCenter) {
            GameObject closestColliderGo = null;
            var minDistanceSquared = float.MaxValue;

            for (var i = 0; i < numFound; i++) {
                var currentColliderGo = foundColliders[i].gameObject;

                // Continue if the current collider go is the root reference
                if (ReferenceEquals(currentColliderGo, _parentGameObject)) {
                    continue;
                }

                var closestColliderPoint = foundColliders[i].ClosestPoint(cellCenter);
                var currentDistanceSquared = (closestColliderPoint - (Vector2) _parentGameObject.transform.position).sqrMagnitude;

                if (currentDistanceSquared >= minDistanceSquared) {
                    continue;
                }

                // Checks if our colliders contain a detectable object
                var index = -1;
                for (var ii = 0; ii < _detectableTags.Length; ii++) {
                    if (currentColliderGo.CompareTag(_detectableTags[ii])) {
                        index = ii;
                        break;
                    }
                }

                if (index > -1 && currentDistanceSquared < minDistanceSquared) {
                    minDistanceSquared = currentDistanceSquared;
                    closestColliderGo = currentColliderGo;
                }
            }

            if (!ReferenceEquals(closestColliderGo, null)) {
                ProcessDetectedObject(closestColliderGo, cellIndex);
            }
        }

        private void ProcessDetectedObject(GameObject detectedObject, int cellIndex)
        {
            for (var i = 0; i < _detectableTags.Length; i++)
            {
                if (!ReferenceEquals(detectedObject, null) && detectedObject.CompareTag(_detectableTags[i]))
                {
                    Array.Clear(_cellDataBuffer, 0, _cellDataBuffer.Length);
                    GetObjectData(detectedObject, i, _cellDataBuffer);
                    ValidateValues(_cellDataBuffer, detectedObject);
                    Array.Copy(_cellDataBuffer, 0, _perceptionBuffer, cellIndex * _cellObservationSize, _cellObservationSize);
                    break;
                }
            }
        }
        
        protected virtual void GetObjectData(GameObject detectedObject, int tagIndex, float[] dataBuffer)
        {
            dataBuffer[0] = tagIndex + 1;
        }
        
        public int Write(ObservationWriter writer) {
            int index = 0;
            for (var h = _cellCount.y - 1; h >= 0; h--) {
                for (var w = 0; w < _cellCount.x; w++) {
                    for (var d = 0; d < _cellObservationSize; d++) {
                        writer[h, w, d] = _perceptionBuffer[index];
                        index++;
                    }
                }
            }

            return index;
        }
        
        public byte[] GetCompressedObservation() {
            var allBytes = new List<byte>();
            var numImages = (_cellObservationSize + 2) / 3;
            for (int i = 0; i < numImages; i++)
            {
                var channelIndex = 3 * i;
                GridValuesToTexture(channelIndex, Math.Min(3, _cellObservationSize - channelIndex));
                allBytes.AddRange(_perceptionTexture.EncodeToPNG());
            }

            return allBytes.ToArray();
        }

        private void GridValuesToTexture(int channelIndex, int numChannelsToAdd)
        {
            for (int i = 0; i < _numCells; i++)
            {
                for (int j = 0; j < numChannelsToAdd; j++)
                {
                    _perceptionColors[i][j] = _perceptionBuffer[i * _cellObservationSize + channelIndex + j];
                }
            }
            _perceptionTexture.SetPixels(_perceptionColors);
        }

        private void ValidateValues(float[] dataValues, GameObject detectedObject) {
            if (_compressionType != SensorCompressionType.PNG) {
                return;
            }

            for (int j = 0; j < dataValues.Length; j++) {
                if (dataValues[j] < 0 || dataValues[j] > 1)
                    throw new UnityAgentsException($"When using compression type {_compressionType} the data value has to be normalized between 0-1. " +
                                                   $"Received value[{dataValues[j]}] for {detectedObject.name}");
            }
        }
        
        public virtual void Reset() {
        }

        public virtual string GetName() {
            return "GridSensor2D";
        }
    }
}
