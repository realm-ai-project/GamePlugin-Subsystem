using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
    public class GridSensor2DComponent : SensorComponent {
        [SerializeField] private Vector2 _cellSize = new Vector2(1, 1);
        [SerializeField] private Vector2Int _cellCount = new Vector2Int(16, 16);
        [SerializeField] private string[] _detectableTags = null;
        [SerializeField] private LayerMask _colliderMask = default;
        [SerializeField] private bool _showGizmos = false;
        [SerializeField] private Color[] _gizmoColors = null;

        private GridSensor2D _debugSensor = null;

        public override ISensor[] CreateSensors() {
            _debugSensor = new GridSensor2D(_cellSize, _cellCount, _detectableTags, gameObject, _colliderMask);
            return new ISensor[] {new OneHotGridSensor2D(_cellSize, _cellCount, _detectableTags, gameObject, _colliderMask)};
        }

        private void OnDrawGizmos() {
            if (!_showGizmos)
                return;

            if (_debugSensor == null)
                return;

            _debugSensor.Update();
            var cellColors = _debugSensor.PerceptionBuffer;
            var scale = new Vector3(_cellSize.x, _cellSize.y, 1);
            var oldGizmoMatrix = Gizmos.matrix;
            for (var i = 0; i < _debugSensor.PerceptionBuffer.Length; i++) {
                var cellPosition = (Vector3)_debugSensor.CellPositions[i] + transform.position;
                var cubeTransform = Matrix4x4.TRS(cellPosition, Quaternion.identity, scale);
                Gizmos.matrix = oldGizmoMatrix * cubeTransform;
                var colorIndex = Mathf.RoundToInt(cellColors[i]) - 1;
                var debugRayColor = Color.white;
                if (colorIndex > -1 && _gizmoColors.Length > colorIndex) {
                    debugRayColor = _gizmoColors[colorIndex];
                }

                Gizmos.color = new Color(debugRayColor.r, debugRayColor.g, debugRayColor.b, .5f);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            Gizmos.matrix = oldGizmoMatrix;
        }
    }
}