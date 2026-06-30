namespace Xarbrough.SelectionUtility
{
	using UnityEngine;

	internal static class CustomDebug
	{
		public const string ConditionString = "SELECTION_UTILITY_DEBUG";

		[System.Diagnostics.Conditional(ConditionString)]
		public static void Log(string message)
		{
			Debug.Log(message);
		}
	}
}