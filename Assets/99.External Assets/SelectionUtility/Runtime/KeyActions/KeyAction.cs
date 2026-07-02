namespace Xarbrough.SelectionUtility
{
	using System;
	using JetBrains.Annotations;
	using UnityEngine;

	/// <summary>
	/// A customizable action that can be executed when a key is pressed in the selection popup.
	/// </summary>
	[PublicAPI]
	public class KeyAction
	{
		/// <summary>
		/// Returns true if the action should be executed for the current context of the KeyDown event.
		/// </summary>
		/// <example>
		/// <code>
		/// ShouldExecute = context => context.Event.keyCode == KeyCode.A && context.HoverItem != null;
		/// </code>
		/// </example>
		public Func<Context, bool> ShouldExecute = _ => true;

		/// <summary>
		/// Implements the action to be taken when the key is pressed.
		/// </summary>
		public Action<Context> Action;

		public readonly struct Context
		{
			/// <summary>
			/// The current IMGUI event.
			/// </summary>
			public readonly Event Event;

			/// <summary>
			/// The item that is currently hovered in the selection popup or null if nothing is hovered.
			/// </summary>
			public readonly GameObject HoverItem;

			/// <summary>
			/// Refreshes all items in all list views of the selection popup.
			/// </summary>
			public readonly Action RefreshItems;

			/// <summary>
			/// Closes the popup window.
			/// </summary>
			public readonly Action CloseWindow;

			internal Context(Event current, GameObject hoverItem, Action refreshItems, Action closeWindow)
			{
				Event = current;
				HoverItem = hoverItem;
				RefreshItems = refreshItems;
				CloseWindow = closeWindow;
			}
		}

		internal void HandleKeyDown(Event current, Context context)
		{
			try
			{
				if (ShouldExecute(context))
				{
					current.Use();
					Action.Invoke(context);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}