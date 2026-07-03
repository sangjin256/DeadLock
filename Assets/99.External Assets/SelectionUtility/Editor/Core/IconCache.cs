namespace Xarbrough.SelectionUtility
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using Object = UnityEngine.Object;

	internal sealed class IconCache
	{
		/// <summary>
		/// Caches the list of component icons for each GameObject to improve performance.
		/// </summary>
		private readonly Dictionary<GameObject, Texture2D[]> iconLookup = new(32);

		private readonly Dictionary<GameObject, Texture2D> miniThumbnails = new(32);

		/// <summary>
		/// The set of icons displayed for the current target.
		/// </summary>
		private readonly List<Texture2D> displayedIcons = new();

		private GameObject currentTarget;

		private readonly List<Component> components = new(8);

		private readonly int defaultAssetHash = "DefaultAsset".GetHashCode();

		public IconCache(IEnumerable<GameObject> gameObjects)
		{
			foreach (GameObject gameObject in gameObjects)
				CacheIcons(gameObject);
		}

		private void CacheIcons(GameObject gameObject)
		{
			gameObject.GetComponents(components);
			displayedIcons.Clear();
			currentTarget = gameObject;

			for (int j = 0; j < components.Count; j++)
				CacheIcon(components[j]);

			iconLookup.Add(currentTarget, displayedIcons.ToArray());
		}

		private void CacheIcon(Object component)
		{
			Texture2D icon;
			string typeName;

			if (component != null)
			{
				icon = AssetPreview.GetMiniThumbnail(component);
				typeName = component.GetType().Name;
			}
			else if (true)
			{
				icon = AssetPreview.GetMiniTypeThumbnail(typeof(DefaultAsset));
				typeName = "DefaultAsset";
			}

			if (icon == null)
				return;

			if (UserPrefs.HiddenIconTypeNames.Contains(typeName))
				return;

			// The default asset icon is returned if nothing else was found,
			// and since it doesn't add much info, omit it.
			if (icon.name.GetHashCode() == defaultAssetHash)
				return;

			displayedIcons.Add(icon);
		}

		/// <summary>
		/// Returns the collection of icons for the provided GameObject.
		/// </summary>
		public Texture2D[] ForGameObject(GameObject gameObject)
		{
			if (!iconLookup.TryGetValue(gameObject, out Texture2D[] icons))
			{
				CacheIcons(gameObject);
				return iconLookup[gameObject];
			}
			else
			{
				return icons;
			}
		}

		public int GetIconCount(GameObject item) => ForGameObject(item).Length;

		public Texture2D GetMiniThumbnail(GameObject gameObject)
		{
			if (!miniThumbnails.TryGetValue(gameObject, out Texture2D thumbnail))
			{
				thumbnail = AssetPreview.GetMiniThumbnail(gameObject);
				miniThumbnails.Add(gameObject, thumbnail);
			}

			return thumbnail;
		}
	}
}