namespace Xarbrough.SelectionUtility
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using UnityEditor;
	using UnityEditor.IMGUI.Controls;
	using UnityEngine;

	/// <summary>
	/// Holds the collection of GameObjects that can be picked and
	/// applies search string filtering to it.
	/// </summary>
	/// <remarks>
	/// Remember that GameObjects in these lists can be destroyed and become null at any time.
	/// </remarks>
	internal class DataSource
	{
		public Action Changed;

		/// <summary>
		/// The original collection of all GameObjects at the mouse position.
		/// </summary>
		public readonly IList<GameObject> Items;

		/// <summary>
		/// The collection of GameObjects after the search filter has been applied.
		/// </summary>
		public readonly List<ListItem> FilteredItems;

		private int FilterIndex
		{
			get => filterIndex;
			set
			{
				if (value == filterIndex)
					return;

				filterIndex = Mathf.Clamp(value, 0, filters.Count);
				EditorPrefs.SetString("SelectionUtility.DataSourceFilter", filters[value].ShortName);
				OnSearchChanged();
			}
		}

		private int filterIndex;

		private DataFilter CurrentFilter => filters[filterIndex];

		/// <summary>
		/// All currently used filters. At runtime start, the list contains the default
		/// filters, but clients may add new ones during InitializeOnLoad.
		/// </summary>
		private static List<DataFilter> filters;

		private static readonly List<DataFilter> defaultFilters = new()
		{
			// Default filter that lets everything through.
			DataFilter.PassThrough,

			// Models, characters and primitives.
			new DataFilter("3D", go =>
				go.GetComponent<MeshRenderer>() != null ||
				go.GetComponent<SkinnedMeshRenderer>() != null),

			// A category for sprites.
			new DataFilter("2D", go => go.GetComponent<SpriteRenderer>() != null),

			// For the Unity 4.6 UI consider everything that has a RectTransform.
			// This will include 3D objects that are part of the UI hierarchy.
			// Alternatively we could only consider CanvasRenderer.
			new DataFilter("UI", go => go.transform is RectTransform),
		};

		private string searchString = string.Empty;
		private string searchStringPrevious = string.Empty;
		private readonly SearchField searchField = new();
		private readonly GUIContent[] filterNames;

		private static GUI.ToolbarButtonSize buttonSize
		{
			get
			{
				if (filters.All(x => x.ShortName.Length < 5))
					return GUI.ToolbarButtonSize.Fixed;
				else
					return GUI.ToolbarButtonSize.FitToContents;
			}
		}

		public DataSource(IList<GameObject> options)
		{
			Items = options;
			FilteredItems = options.Select(x => new ListItem(x, x.name)).ToList();

			filters = CustomOrDefaultFilters();
			filterIndex = RefreshSelectedFilter(filters);
			filterNames = filters.Select(x => new GUIContent(x.ShortName)).ToArray();

			OnSearchChanged();
		}

		private static List<DataFilter> CustomOrDefaultFilters()
		{
			List<DataFilter> result = defaultFilters;

			if (SelectionPopupExtensions.FilterModifier != null)
			{
				var customFilters = SelectionPopupExtensions.FilterModifier
					.Invoke(defaultFilters.ToList())
					.Where(x => x != null).ToList();

				if (customFilters.Count == 0)
				{
					Debug.LogError("Must provide at least one filter. " +
					               "Turn off the filter toolbar via the preferences item " +
					               "if you wish to disable filtering entirely.");
				}
				else
				{
					result = customFilters;
				}
			}

			return result;
		}

		private static int RefreshSelectedFilter(List<DataFilter> filters)
		{
			string selectedFilter = EditorPrefs.GetString("SelectionUtility.DataSourceFilter", "All");
			int index = filters.FindIndex(x => x.ShortName == selectedFilter);
			return index != -1 ? index : 0;
		}

		public static float MinimumWidth()
		{
			float size = 0f;

			for (int i = 0; i < filters.Count; i++)
			{
				float width = EditorStyles.miniButton.CalcSize(
					new GUIContent(filters[i].ShortName)).x;

				if (buttonSize == GUI.ToolbarButtonSize.Fixed)
				{
					if (width > size)
						size = width;
				}
				else
				{
					size += width;
				}
			}

			if (buttonSize == GUI.ToolbarButtonSize.Fixed)
				return size * filters.Count;
			else
				return size;
		}

		public void SearchFieldGUI(Rect rect, float height)
		{
			Rect searchRect = rect;
			searchRect.height = height;
			searchRect.yMin += 2;
			searchRect.xMin += 4;
			searchRect.xMax -= 3;

			EditorGUI.BeginChangeCheck();
			searchString = searchField.OnToolbarGUI(searchRect, searchString);

			// Because of an issue in Unity 2020.2.1 this additional check is required.
			if (EditorGUI.EndChangeCheck() || searchString != searchStringPrevious)
			{
				searchStringPrevious = searchString;
				OnSearchChanged();
			}
		}

		public void DrawFilterModes(Rect rect)
		{
			DrawToolbar(rect);
		}

		private void DrawToolbar(Rect rect)
		{
			GUILayout.Space(3f);
			GUILayout.BeginHorizontal(GUILayout.MaxWidth(rect.width));
			GUILayout.FlexibleSpace();

			// Due to a small layout issue the toolbar would be
			// offset from the center otherwise.
			GUILayout.Space(2f);
			if (buttonSize == GUI.ToolbarButtonSize.FitToContents)
				GUILayout.Space(4f);

			FilterIndex = GUILayout.Toolbar(
				FilterIndex,
				filterNames,
				GUI.skin.button,
				buttonSize);

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private void OnSearchChanged()
		{
			// To polish the search experience:
			// - Remove white space at the start and end of the search string.
			// - Ignore multiple spaces in a row by collapsing them down to a single one.
			// - Ignore letter case.
			string value = Regex.Replace(searchString.Trim(), "[ ]+", " ");

			// Extract any component type query. User must enter 't:' followed by a type name,
			// with or without spaces in between. Also match if no type name is specified (yet)
			// so that the search window shows all entries until the type name is started.
			var match = Regex.Match(value, @"t:\s*(\w*)");
			string typeName = null;

			if (match.Success)
			{
				value = value.Replace(match.Value, string.Empty).Trim();
				typeName = match.Groups[1].Value.Trim().ToLower();
			}

			RefreshFilteredItems(typeName, value);
		}

		private void RefreshFilteredItems(string typeName, string searchValue)
		{
			FilteredItems.Clear();
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i] == null)
					continue;

				// It would be possible to use GetComponent(string)
				// to check for type match, but this would be case-sensitive.
				// For case-insensitive lookup, compare each component name.
				bool hasMatchingComponent = false;
				if (typeName != null)
				{
					var components = Items[i].GetComponents<Component>();
					if (components.Any(comp => comp != null && comp.GetType().Name.ToLower() == typeName))
					{
						hasMatchingComponent = true;
					}
				}

				if (Items[i].name.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0 &&
				    (typeName == null || hasMatchingComponent) &&
				    CurrentFilter.IsAllowed(Items[i]))
				{
					FilteredItems.Add(new ListItem(Items[i], Items[i].name));
				}
			}

			Changed?.Invoke();
		}
	}
}