namespace Xarbrough.SelectionUtility
{
	using System.Collections.Generic;

	/// <summary>
	/// A method that modifies the default filters.
	/// Must return at least one item.
	/// The provided default filters list can be modified (e.g. add, remove, replace, reorder)
	/// and used as the return value.
	/// </summary>
	/// <param name="defaultFilters">
	/// A copy of the unaltered list of default filters. Can be modified safely.
	/// </param>
	public delegate IEnumerable<DataFilter> FilterModifier(List<DataFilter> defaultFilters);
}