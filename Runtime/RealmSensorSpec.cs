using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealmAI {
	public enum SensorDataType {
		None,
		Float,
		Int,
		Bool,
		Vector2,
		Vector3,
	}
    
	[Serializable]
	public class RealmSensorSpec {
		// This could have been a single generic func but... XD
		public string Label = "Custom Sensor";
		public SensorDataType DataType = SensorDataType.Float;
		public SerializedFloatFunc FloatFunc = default;
		public SerializedIntFunc IntFunc = default;
		public SerializedBoolFunc BoolFunc = default;
		public SerializedVector2Func Vector2Func = default;
		public SerializedVector3Func Vector3Func = default;
		public int IntMin = 0;
		public int IntMaxExclusive = 1;
	}
}