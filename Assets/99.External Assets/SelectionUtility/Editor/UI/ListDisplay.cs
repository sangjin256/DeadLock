namespace Xarbrough.SelectionUtility
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.UIElements;

	internal class ListDisplay
	{
		public event Action<GameObject> OnHover = delegate { };
		public event Action<GameObject, Vector2> OnExitHover = delegate { };
		public event Action<GameObject> OnSelectionChanged = delegate { };

		public const float MainIconSize = 18;
		public const float ComponentIconSize = 16;
		public const float RowHeight = 20;
		public const float RowPadding = 4;

		private readonly ListView listView;
		private readonly IconCache iconCache;

		private float maxLabelWidth;

		public ListDisplay(ListView listView, IconCache iconCache)
		{
			this.listView = listView;
			this.iconCache = iconCache;

			listView.makeItem = MakeItem;
			listView.bindItem = BindItem;

			listView.fixedItemHeight = RowHeight;
			listView.selectionType = SelectionType.Single;
#if UNITY_2022_2_OR_NEWER
			listView.selectionChanged +=
#else
			listView.onSelectionChange +=
#endif
				OnSelectionChangedImpl;
		}

		private void OnSelectionChangedImpl(IEnumerable<object> selection)
		{
			OnSelectionChanged.Invoke(((ListItem)selection.First()).GameObject);
		}

		private VisualElement MakeItem()
		{
			var row = new VisualElement();

			row.RegisterCallback<MouseEnterEvent>(evt =>
			{
				GameObject item = GetItem((VisualElement)evt.target);
				OnHover(item);
			});

			row.RegisterCallback<MouseLeaveEvent>(evt =>
			{
				GameObject item = GetItem((VisualElement)evt.target);
				OnExitHover(item, evt.mousePosition);
			});

			row.style.flexDirection = FlexDirection.Row;
			row.style.alignItems = Align.Center;
			row.style.paddingLeft = RowPadding / 2f;
			row.style.paddingRight = RowPadding / 2f;
			row.Add(new VisualElement
			{
				name = "MainIcon",
				style =
				{
					width = MainIconSize,
					height = MainIconSize,
					flexShrink = 0,
				},
			});
			row.Add(new IMGUIContainer { name = "Label" });
			VisualElement componentIcons = new VisualElement
			{
				name = "ComponentIcons",
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
				},
			};

			for (int i = 0; i < 2; i++)
				componentIcons.Add(CreateComponentIcon(DisplayStyle.None));

			row.Add(componentIcons);
			row.Add(new VisualElement { name = "ComponentIcon" });
			return row;

			GameObject GetItem(VisualElement element)
			{
				int index = (int)element.userData;
				return ((ListItem)listView.itemsSource?[index]!).GameObject;
			}
		}

		public void Clear()
		{
			listView.itemsSource = null;
			listView.RefreshItems();
		}

		public void Refresh() => listView.RefreshItems();

		public static float CalculateFullWidth(IList<GameObject> items, IconCache iconCache)
		{
			float maxIconsWidth = items
				.Select(item => iconCache.GetIconCount(item) * ComponentIconSize)
				.Prepend(0f)
				.Max();

			float maxLabelWidth = CalculateLabelWidth(items);

			return MainIconSize + maxLabelWidth + maxIconsWidth + RowPadding;
		}

		public static float CalculateLabelWidth(IEnumerable<GameObject> items)
		{
			float maxLabelWidth = items
				.Select(item => new { item, style = Styles.LabelStyle(item) })
				.Select(t => t.style.CalcSize(new GUIContent(t.item.name)).x)
				.Prepend(0f)
				.Max();

			if (maxLabelWidth > 0f)
				maxLabelWidth += 2f; // Padding to the right of the label.

			return maxLabelWidth;
		}

		public void SetSource(IList<ListItem> itemsSource, bool isHierarchy = false)
		{
#if UNITY_2022_3_OR_NEWER
			// In Unity 2022, assigning the itemsSource will trigger selectionChange.
			listView.selectionChanged -= OnSelectionChangedImpl;
#endif

			if (isHierarchy)
				listView.itemsSource = itemsSource.Count > 1 ? (IList)itemsSource : null;
			else
				listView.itemsSource = (IList)itemsSource;

#if UNITY_2022_3_OR_NEWER
			listView.selectionChanged += OnSelectionChangedImpl;
#endif

			maxLabelWidth = CalculateLabelWidth(itemsSource.Select(x => x.GameObject));

			if (isHierarchy)
				listView.SetSelectionWithoutNotify(ArrayCache<int>.Single(itemsSource.Count - 1));

			listView.RefreshItems();
		}

		private static VisualElement CreateComponentIcon(DisplayStyle display)
		{
			return new VisualElement
			{
				name = "ComponentIcon",
				style =
				{
					display = display,
					width = ComponentIconSize,
					height = ComponentIconSize,
					flexShrink = 0,
				},
			};
		}

		private void BindItem(VisualElement element, int index)
		{
			element.userData = index;
			ListItem item = (ListItem)listView.itemsSource[index];

			DrawMainIcon(element, item.GameObject);
			DrawLabel(element, item.GameObject, item.Name);
			DrawComponentIcons(element, item.GameObject);
		}

		private void DrawMainIcon(VisualElement element, GameObject item)
		{
			Texture2D icon = iconCache.GetMiniThumbnail(item);
			element.Q("MainIcon").style.backgroundImage = icon;
		}

		private void DrawLabel(VisualElement element, GameObject item, string name)
		{
			var labelStyle = Styles.LabelStyle(item);
			element.name = name;

			var labelContainer = element.Q<IMGUIContainer>();
			labelContainer.style.width = maxLabelWidth;
			labelContainer.onGUIHandler = () =>
			{
				EditorGUILayout.LabelField(name, labelStyle);

				if (item == null)
				{
					// Draw a strikethrough line to indicate deleted items.
					Vector2 textSize = labelStyle.CalcSize(new GUIContent(name));
					Rect lastRect = GUILayoutUtility.GetLastRect();
					EditorGUI.DrawRect(new Rect(lastRect.x, lastRect.y + textSize.y / 2, textSize.x, 1), Color.red);
				}
			};
		}

		private void DrawComponentIcons(VisualElement element, GameObject item)
		{
			Texture2D[] icons = iconCache.ForGameObject(item);
			var iconParent = element.Q("ComponentIcons");

			// Update existing icons.
			for (int i = 0; i < Mathf.Min(icons.Length, iconParent.childCount); i++)
			{
				IStyle style = iconParent.ElementAt(i).style;
				style.display = DisplayStyle.Flex;
				style.backgroundImage = icons[i];
			}

			// Add new icons.
			for (int i = iconParent.childCount; i < icons.Length; i++)
			{
				var iconElement = CreateComponentIcon(DisplayStyle.Flex);
				iconElement.style.backgroundImage = icons[i];
				iconParent.Add(iconElement);
			}

			// Disable extra icons.
			for (int i = icons.Length; i < iconParent.childCount; i++)
			{
				iconParent.ElementAt(i).style.display = DisplayStyle.None;
			}
		}
	}
}