// To activate the extension sample, uncomment the next line:
#define ENABLE_EXTENSION_SAMPLE

#if !ENABLE_EXTENSION_SAMPLE && UNITY_EDITOR
namespace Xarbrough.SelectionUtility.Samples
{
	using UnityEditor;
	using UnityEngine;

	[InitializeOnLoad]
	internal static class CustomKeyAction
	{
		static CustomKeyAction()
		{
			// Optionally, remove all default key actions.
			SelectionPopupExtensions.KeyActions.Clear();

			// This action pings and selects the prefab asset if the hovered GameObject is a prefab instance.
			SelectionPopupExtensions.KeyActions.Add(new KeyAction
			{
				// This delegate should return true, when the action should be executed.
				ShouldExecute = context => context.Event.keyCode == KeyCode.A &&
				                         context.HoverItem != null &&
				                         PrefabUtility.IsPartOfPrefabInstance(context.HoverItem),
				Action = context =>
				{
					var prefab = PrefabUtility.GetCorrespondingObjectFromSource(context.HoverItem);
					if (prefab != null)
					{
						Selection.activeObject = prefab;
						EditorGUIUtility.PingObject(prefab);
					}
				},
			});
		}
	}
}
#endif