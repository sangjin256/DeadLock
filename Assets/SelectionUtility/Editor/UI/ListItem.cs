namespace Xarbrough.SelectionUtility
{
	using UnityEngine;

	internal struct ListItem
	{
		public readonly GameObject GameObject;
		public readonly string Name;

		public ListItem(GameObject gameObject, string name)
		{
			GameObject = gameObject;
			Name = name;
		}
	}
}