namespace Xarbrough.SelectionUtility
{
	using System;
	using UnityEngine;

	/// <summary>
	/// An option in the filter toolbar.
	/// </summary>
	/// <example>
	/// A simple data filter that only show GameObjects
	/// that have a specific tag assigned.
	/// <code>
	/// using Xarbrough.SelectionUtility;
	/// using System.Collections.Generic;
	/// using UnityEditor;
	/// 
	/// public static class SelectionUtilityExtension
	/// {
	/// 	[InitializeOnLoadMethod]
	/// 	private static void RegisterCustomFilter()
	/// 	{
	/// 		SelectionPopupExtensions.FilterModifier = AddCustomFilter;
	/// 	}
	/// 
	/// 	private static IEnumerable&lt;DataFilter &gt; AddCustomFilter(List&lt;DataFilter &gt; filters)
	/// 	{
	/// 		filters.Add(new DataFilter("Obstacles", go => go.CompareTag("Obstacle")));
	/// 		return filters;
	/// 	}
	/// }
	/// </code>
	/// </example>
	public sealed class DataFilter
	{
		/// <summary>
		/// The display name and unique identifier of the filter.
		/// </summary>
		/// <exception cref="ArgumentException">If the provided value is null or empty.</exception>
		public string ShortName
		{
			get => shortName;
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentException($"{nameof(ShortName)} cannot be null or empty.");

				shortName = value;
			}
		}

		private string shortName;

		/// <summary>
		/// The filter function that is run on each GameObject
		/// in the list of all objects under the mouse.
		/// </summary>
		/// <exception cref="ArgumentException">If the provided value is null.</exception>
		public FilterFunction Filter
		{
			get => filter;
			set => filter = value ?? throw new ArgumentException(
				$"{Filter} cannot be null. Use a no-op implementation to disable the filter.");
		}

		private FilterFunction filter;

		public DataFilter(string name, FilterFunction filter)
		{
			ShortName = name;
			Filter = filter;
		}

		internal bool IsAllowed(GameObject go)
		{
			return Filter.Invoke(go);
		}

		/// <summary>
		/// A filter named "All" that allows any item.
		/// </summary>
		public static readonly DataFilter PassThrough = new ("All", _ => true);
	}
}