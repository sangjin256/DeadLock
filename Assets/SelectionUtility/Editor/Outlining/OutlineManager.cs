namespace Xarbrough.SelectionUtility
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	internal class OutlineManager
	{
		private readonly List<OutlineRenderer> renderers = new();
		private GameObject target;

		/// <summary>
		/// Ensures that the provided GameObject is outlined
		/// in the scene view. Safe to call multiple times in a row.
		/// </summary>
		public void SetOutlineTarget(GameObject target)
		{
			if (UserPrefs.EnableHoverSelectionOutline == false)
				return;

			if (target == this.target)
				return;

			Clear();

			this.target = target;

			foreach (Camera camera in SceneView.GetAllSceneCameras())
			{
				if (camera == null)
					continue;

				var renderer = new OutlineRenderer(camera)
				{
					BlurRadius = UserPrefs.OutlineThickness,
					OutlineColor = UserPrefs.OutlineColor,
				};
				var rendererComponents = target.GetComponents<Renderer>();
				renderer.AddTargets(rendererComponents);
				renderers.Add(renderer);
			}
		}

		public void Clear()
		{
			target = null;

			if (renderers.Count > 0)
			{
				foreach (var renderer in renderers)
					renderer.Clear();

				renderers.Clear();
				SceneView.RepaintAll();
			}
		}
	}
}