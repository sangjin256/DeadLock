namespace Xarbrough.SelectionUtility
{
	using UnityEngine;

	/// <summary>
	/// Returns true if the GameObject should be shown
	/// within the associated filter toolbar tab.
	/// A filter function is set on a <see cref="DataFilter"/>
	/// instance and called automatically when the selection
	/// popup shows the associated filter tab.
	/// </summary>
	/// <example>
	/// A simple filter function that only allows GameObjects
	/// with a specific tagged.
	/// <code>
	/// private static bool HasTag(GameObject go)
	/// {
	/// 	return go.CompareTag("MyTag");
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Filter functions can be set in the <see cref="DataFilter"/> via lambda expressions.
	/// <code>
	/// var filter = new DataFilter(
	/// 	"Colliders", 
	/// 	go => go.GetComponent&lt;Collider&gt;() != null);
	/// </code>
	/// </example>
	public delegate bool FilterFunction(GameObject go);
}