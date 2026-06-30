namespace Xarbrough.SelectionUtility
{
	using UnityEditor;
	using UnityEngine;

	internal class IntSetting : Setting<int>
	{
		private readonly bool clamp;
		private readonly int minValue;
		private readonly int maxValue;

		public IntSetting(string key, int defaultValue, int min, int max) : base(key, defaultValue)
		{
			minValue = min;
			maxValue = max;
			clamp = true;
		}

		protected override int DrawProperty(GUIContent label, int value)
		{
			return EditorGUILayout.IntField(label, value);
		}

		public override int Value
		{
			get => base.Value;
			protected set
			{
				if (clamp)
					value = Mathf.Clamp(value, minValue, maxValue);

				base.Value = value;
			}
		}

		protected override int LoadValue()
		{
			return EditorPrefs.GetInt(Key, DefaultValue);
		}

		protected override void SaveValue(int value)
		{
			EditorPrefs.SetInt(Key, value);
		}
	}
}
