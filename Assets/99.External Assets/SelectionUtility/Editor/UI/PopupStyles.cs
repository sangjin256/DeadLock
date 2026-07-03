namespace Xarbrough.SelectionUtility
{
	using UnityEditor;
	using UnityEngine;

	internal static class Styles
	{
		public static void Init()
		{
			prefabLabel = new GUIStyle("PR PrefabLabel");
			prefabLabel.alignment = TextAnchor.MiddleLeft;
			RectOffset prefabLabelPadding = prefabLabel.padding;
			prefabLabelPadding.top += 2;
			prefabLabel.padding = prefabLabelPadding;

			if (EditorGUIUtility.isProSkin == false)
			{
				// Darken the blue color in light skin.
				var col = prefabLabel.normal.textColor;
				col *= 0.5f;
				col.a = 1f;
				prefabLabel.normal.textColor = col;
			}

			label = new GUIStyle(EditorStyles.label);
			label.alignment = TextAnchor.MiddleLeft;
			RectOffset padding = label.padding;
			padding.top -= 1;
			padding.left -= 1;
			label.padding = padding;
		}

		public static GUIStyle LabelStyle(GameObject target)
		{
			if (IsPrefab(target))
				return prefabLabel;
			else
				return label;
		}

		private static bool IsPrefab(Object target)
		{
			if (target == null)
				return false;

			return PrefabUtility.IsPartOfAnyPrefab(target);
		}

		private static GUIStyle label;
		private static GUIStyle prefabLabel;
	}
}