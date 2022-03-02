using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealmAI {
	public enum ActionDataType {
		None,
		Float,
		Int,
		Bool,
		Vector2,
		Vector3,
	}
    
	[Serializable]
	public class RealmActionSpec {
		// This could have been a single generic action/func but... XD
		public string Label = "Action";
		public ActionDataType DataType = ActionDataType.Float;
		public SerializedFloatAction FloatCallback = default;
		public SerializedFloatFunc FloatHeuristic = default;
		public SerializedIntAction IntCallback = default;
		public SerializedIntFunc IntHeuristic = default;
		public SerializedBoolAction BoolCallback = default;
		public SerializedBoolFunc BoolHeuristic = default;
		public SerializedVector2Action Vector2Callback = default;
		public SerializedVector2Func Vector2Heuristic = default;
		public SerializedVector3Action Vector3Callback = default;
		public SerializedVector3Func Vector3Heuristic = default;
		public int IntMaxExclusive = 1;
	}
}
