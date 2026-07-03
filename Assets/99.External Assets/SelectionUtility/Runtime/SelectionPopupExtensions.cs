using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Xarbrough.SelectionUtility.Editor")]

namespace Xarbrough.SelectionUtility
{
	using System.Collections.Generic;
	using JetBrains.Annotations;

	[PublicAPI]
	public static class SelectionPopupExtensions
	{
		/// <summary>
		/// Changes the available filters.
		/// </summary>
		/// <example>
		/// A simple modifier that adds a new filter to the
		/// list of default filters.
		/// <code>
		/// using Xarbrough.SelectionUtility;
		/// using UnityEditor;
		/// 
		/// public static class SelectionUtilityExtension
		/// {
		/// 	[InitializeOnLoadMethod]
		/// 	private static void RegisterCustomFilter()
		/// 	{
		/// 		SelectionPopupExtensions.FilterModifier = filters =>
		/// 		{
		/// 			filters.Add(new DataFilter("Obstacles", go => go.CompareTag("Obstacle")));
		/// 			return filters;
		/// 		};
		/// 	}
		/// }
		/// </code>
		/// </example>
		public static FilterModifier FilterModifier { get; set; }

		/// <summary>
		/// Modify this list to add or remove custom keyboard interactions with the selection popup.
		/// </summary>
		public static List<KeyAction> KeyActions { get; } = new()
		{
			new DeleteAction(),
			new OpenPrefabAction(),
		};
	}
}