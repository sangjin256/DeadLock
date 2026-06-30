namespace Xarbrough.SelectionUtility
{
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// Handles the selection of GameObjects from within the popup list.
	/// </summary>
	internal static class ObjectSelector
	{
		public static void SelectObject(Event current, Object selectedObject)
		{
			if (current.shift || current.control)
				ToggleSelectedObjectAdditive(selectedObject);
			else
				SelectObject(selectedObject);
		}

		private static void SelectObject(Object selectedObject)
		{
			if (Selection.activeObject != selectedObject)
				Selection.activeObject = selectedObject;
			else
				EditorGUIUtility.PingObject(selectedObject);
		}

		private static void ToggleSelectedObjectAdditive(Object selectedObject)
		{
			var selectedObjects = Selection.objects;

			if (ArrayUtility.Contains(selectedObjects, selectedObject))
				ArrayUtility.Remove(ref selectedObjects, selectedObject);
			else
				ArrayUtility.Add(ref selectedObjects, selectedObject);

			Selection.objects = selectedObjects;
		}
	}
}