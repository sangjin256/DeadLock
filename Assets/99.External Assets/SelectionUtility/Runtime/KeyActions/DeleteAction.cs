namespace Xarbrough.SelectionUtility
{
#if UNITY_EDITOR
	using UnityEditor;
#endif
	using UnityEngine;

	public sealed class DeleteAction : KeyAction
	{
		public DeleteAction()
		{
			ShouldExecute = context =>
				context.Event.keyCode is KeyCode.Delete or KeyCode.Backspace
				&& context.HoverItem != null;
			Action = context =>
			{
#if UNITY_EDITOR
				GameObject item = context.HoverItem;

				if (PrefabUtility.IsPartOfAnyPrefab(item) &&
				    PrefabUtility.IsAnyPrefabInstanceRoot(item) == false)
				{
					int choice = EditorUtility.DisplayDialogComplex(
						title: "Prefab Part Deletion",
						message: $"'{item.name}' is part of a prefab. " +
						         "Would you like to open prefab mode or delete the root?",
						ok: "Open Prefab Mode",
						cancel: "Cancel",
						alt: "Delete Root"
					);

					if (choice == 0)
					{
						OpenPrefabAction.OpenPrefab(context);
					}
					else if (choice == 2)
					{
						Undo.DestroyObjectImmediate(PrefabUtility.GetNearestPrefabInstanceRoot(item));
						context.RefreshItems();
					}

					return;
				}

				Undo.DestroyObjectImmediate(item);
				context.RefreshItems();
#endif
			};
		}
	}
}