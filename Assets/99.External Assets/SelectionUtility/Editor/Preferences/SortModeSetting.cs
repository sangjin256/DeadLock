namespace Xarbrough.SelectionUtility
{
	using JetBrains.Annotations;
	using UnityEditor;
	using UnityEngine;

	internal class SortModeSetting : Setting<SortMode>
	{
		public SortModeSetting(string key, SortMode defaultValue = default) : base(key, defaultValue)
		{
		}

		protected override SortMode DrawProperty(GUIContent label, SortMode value)
		{
			return (SortMode)EditorGUILayout.EnumPopup(label, value);
		}

		protected override SortMode LoadValue()
		{
			return (SortMode)EditorPrefs.GetInt(Key);
		}

		protected override void SaveValue(SortMode value)
		{
			EditorPrefs.SetInt(Key, (int)value);
		}
	}

	internal enum SortMode
	{
		[UsedImplicitly]
		DrawOrder,
		TransformHierarchy,
	}
}