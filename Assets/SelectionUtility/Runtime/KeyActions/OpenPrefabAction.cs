namespace Xarbrough.SelectionUtility
{
#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.SceneManagement;
#endif
	using UnityEngine;

	public sealed class OpenPrefabAction : KeyAction
	{
		public OpenPrefabAction()
		{
#if UNITY_EDITOR
			ShouldExecute = context => context.Event.keyCode == KeyCode.P &&
			                           context.HoverItem != null &&
			                           PrefabUtility.IsPartOfPrefabInstance(context.HoverItem);
			Action = OpenPrefab;
#endif
		}

		public static void OpenPrefab(Context context)
		{
#if UNITY_EDITOR
			string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.HoverItem);
			Selection.activeGameObject = context.HoverItem;
			PrefabStageUtility.OpenPrefab(path, context.HoverItem);
			context.CloseWindow();
#endif
		}
	}
}