namespace Xarbrough.SelectionUtility
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.UIElements;
	using Object = UnityEngine.Object;

	internal class SelectionUtilityWindow : EditorWindow
	{
		private IList<GameObject> items;
		private IconCache iconCache;
		private DataSource dataSource;
		private ListDisplay mainListDisplay;
		private ListDisplay treeListDisplay;
		private float leftSideWidth;
		private float rightSideWidth;
		private GameObject hoverItem;

		private static float rowHeight => ListDisplay.RowHeight;

		private const float filtersHeight = 24;

		private readonly OutlineManager outlineManager = new();

		private void OnEnable()
		{
			// This is not normally shown to the user, but it could appear as part of the UI Toolkit Debugger or other tools.
			titleContent.text = name = "Selection Utility";
		}

		internal void SetItems(IList<GameObject> items)
		{
			this.items = items;
			iconCache = new IconCache(items);
			dataSource = new DataSource(items);
		}

		internal Vector2 GetWindowSize()
		{
			return new Vector2(
				x: CalculateWindowWidth(),
				y: CalculateWindowHeight()
			);
		}

		private float CalculateWindowWidth()
		{
			leftSideWidth = Mathf.Max(
				ListDisplay.CalculateFullWidth(items, iconCache),
				DataSource.MinimumWidth()
			);

			if (UserPrefs.ShowHierarchy)
			{
				rightSideWidth = CalculateRightSideWidth();
				return leftSideWidth + rightSideWidth;
			}
			else
				return leftSideWidth;
		}

		private float CalculateWindowHeight()
		{
			float height = Mathf.Min(
				Screen.currentResolution.height,
				700,
				rowHeight * items.Count + filtersHeight + rowHeight);

			float minHeight = RightSideVisible() ? filtersHeight + rowHeight * 3 : filtersHeight;
			if (height < minHeight)
				height = minHeight;

			return height;
		}

		public void RefreshItems()
		{
			mainListDisplay.Refresh();
			treeListDisplay.Refresh();
		}

		private float CalculateRightSideWidth()
		{
			float width = items
				.Select(Hierarchy.GetDisplayHierarchy)
				.Where(hierarchy => hierarchy.Count > 1)
				.Select(hierarchy => ListDisplay.CalculateFullWidth(hierarchy, iconCache))
				.Prepend(0f)
				.Max();

			// For extremely short GameObject names, ensure the label "hierarchy" is not cut off.
			if (width > 0f)
				width = Mathf.Max(width, 60f);

			return width;
		}

		private void OnGUI()
		{
			Event current = Event.current;

			if (current.type == EventType.KeyDown)
			{
				if (current.keyCode == KeyCode.Escape)
				{
					current.Use();
					Close();
				}

				var keyContext = new KeyAction.Context
				(
					current,
					hoverItem,
					RefreshItems,
					Close
				);

				foreach (KeyAction keyAction in SelectionPopupExtensions.KeyActions)
				{
					keyAction.HandleKeyDown(current, keyContext);
				}
			}
		}

		private void CreateGUI()
		{
			// If the popup is open during a scene or play-mode state change.
			if (items == null)
			{
				Close();
				return;
			}

			var popup = Assets.LoadFromGuid<VisualTreeAsset>("11ffa0f040e28420391374d2469c1359");
			popup.CloneTree(rootVisualElement);

			SetupMainListView();

			var filters = rootVisualElement.Q<IMGUIContainer>("Filters");
			filters.style.height = filtersHeight;
			filters.onGUIHandler = () =>
			{
				Rect rect = new Rect(0, 0, leftSideWidth, rowHeight);
				dataSource.DrawFilterModes(rect);
			};
			var searchField = rootVisualElement.Q<IMGUIContainer>("SearchField");
			searchField.style.height = rowHeight;
			searchField.onGUIHandler = () =>
			{
				Rect rect = new Rect(0, 0, leftSideWidth, rowHeight);
				dataSource.SearchFieldGUI(rect, rowHeight);
			};
			dataSource.Changed += () =>
			{
				mainListDisplay.SetSource(dataSource.FilteredItems);

				if (hoverItem != null)
				{
					var hierarchyItems = Hierarchy.GetDisplayHierarchy(hoverItem);
					treeListDisplay?.SetSource(hierarchyItems.Select(x => new ListItem(x, x.name)).ToList(), isHierarchy: true);
				}
			};

			var leftSide = rootVisualElement.Q("Left");
			leftSide.style.width = leftSideWidth;

			var rightSide = rootVisualElement.Q("Right");

			if (UserPrefs.ShowHierarchy)
			{
				rightSide.style.width = rightSideWidth;
				rightSide.Q<Label>().style.height = filtersHeight;

				var treeView = rightSide.Q<ListView>();
				treeListDisplay = new ListDisplay(treeView, iconCache);
				treeListDisplay.OnHover += item =>
				{
					hoverItem = item;
					outlineManager.SetOutlineTarget(item);
				};
				treeListDisplay.OnExitHover += OnExitHover;
				treeListDisplay.OnSelectionChanged += OnSelectionChanged;
			}
			else
			{
				rightSide.style.display = DisplayStyle.None;
			}

			if (!RightSideVisible())
			{
				leftSide.style.borderRightWidth = 0;
			}

			Undo.undoRedoPerformed += RefreshItems;
		}

		private bool RightSideVisible()
		{
			return UserPrefs.ShowHierarchy && rightSideWidth > 0;
		}

		private void SetupMainListView()
		{
			var listView = rootVisualElement.Q("Left").Q<ListView>();
			mainListDisplay = new ListDisplay(listView, iconCache);
			mainListDisplay.SetSource(dataSource.FilteredItems);
			mainListDisplay.OnHover += OnHover;
			mainListDisplay.OnExitHover += OnExitHover;
			mainListDisplay.OnSelectionChanged += OnSelectionChanged;
		}

		private void OnHover(GameObject item)
		{
			hoverItem = item;
			if (item != null)
			{
				var hierarchyItems = Hierarchy.GetDisplayHierarchy(item);
				treeListDisplay?.SetSource(hierarchyItems.Select(x => new ListItem(x, x.name)).ToList(), isHierarchy: true);
				outlineManager.SetOutlineTarget(item);
			}
			else
			{
				treeListDisplay?.Clear();
			}
		}

		private void OnExitHover(GameObject item, Vector2 mousePosition)
		{
			hoverItem = null;
			outlineManager.Clear();
		}

		private void OnSelectionChanged(Object selection)
		{
			ObjectSelector.SelectObject(Event.current, selection);
			Close();
		}

		private void OnDestroy()
		{
			outlineManager.Clear();
		}
	}
}