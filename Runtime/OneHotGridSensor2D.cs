using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RealmAI {
	public class OneHotGridSensor2D : GridSensor2D {
		public OneHotGridSensor2D(
			Vector2 cellSize,
			Vector2Int cellCount,
			string[] detectableTags,
			GameObject parentGameObject,
			LayerMask colliderMask) : base(cellSize, cellCount, detectableTags, parentGameObject, colliderMask, SensorCompressionType.PNG) {
		}
		
		protected override int GetCellObservationSize()
		{
			return DetectableTags?.Length ?? 0;
		}

		protected override void GetObjectData(GameObject detectedObject, int tagIndex, float[] dataBuffer)
		{
			dataBuffer[tagIndex] = 1;
		}

		public override string GetName() {
			return "OneHotGridSensor2D";
		}
	}

}