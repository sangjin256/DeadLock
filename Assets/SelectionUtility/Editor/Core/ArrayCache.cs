namespace Xarbrough.SelectionUtility
{
	internal static class ArrayCache<T>
	{
		private static readonly T[] oneItemArray = new T[1];

		public static T[] Single(T element)
		{
			oneItemArray[0] = element;
			return oneItemArray;
		}
	}
}