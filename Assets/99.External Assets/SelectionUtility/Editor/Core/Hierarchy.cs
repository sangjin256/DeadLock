namespace Xarbrough.SelectionUtility
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	internal static class Hierarchy
	{
		public static List<GameObject> GetDisplayHierarchy(GameObject leaf)
		{
			var elements = GetParents(leaf).ToList();
			elements.Add(leaf);
			return CollapseList(elements, maxCount: 32);
		}

		public static IEnumerable<GameObject> GetParents(GameObject go)
		{
			return GetParentsLeafToRoot(go).Reverse();

			static IEnumerable<GameObject> GetParentsLeafToRoot(GameObject go)
			{
				while (go.transform.parent != null)
				{
					go = go.transform.parent.gameObject;
					yield return go;
				}
			}
		}
		
		public static List<T> CollapseList<T>(List<T> list, int maxCount, T omittedValue = default)
		{
			if (maxCount < 3)
				throw new ArgumentException();

			int itemsToRemove = list.Count - maxCount;

			if (itemsToRemove < 1)
				return list;

			int startIndex = (list.Count - itemsToRemove + 1) / 2 - 1;
			list.RemoveRange(startIndex, itemsToRemove + 1);
			list.Insert(startIndex, omittedValue);

			Debug.Assert(list.Count <= maxCount);
			return list;
		}

		public static void Sort(List<GameObject> list)
		{
			list.Sort((x, y) =>
			{
				int xLevel = GetHierarchyLevel(x);
				int yLevel = GetHierarchyLevel(y);
				
				if (xLevel == yLevel)
				{
					return x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex());
				}
				
				return xLevel.CompareTo(yLevel);
			});
		}
		
		private static int GetHierarchyLevel(GameObject go)
		{
			int level = 0;
			Transform current = go.transform;
			while (current.parent != null)
			{
				level++;
				current = current.parent;
			}

			return level;
		}
	}
}