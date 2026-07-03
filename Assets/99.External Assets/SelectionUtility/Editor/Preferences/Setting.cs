namespace Xarbrough.SelectionUtility
{
	using System;
	using UnityEditor;
	using UnityEngine;

	internal interface ISetting
	{
		void DrawProperty();
		void Reset();
	}

	/// <summary>
	/// A single item in the list of <see cref="UserPrefs"/>.
	/// </summary>
	/// <remarks>
	/// Handles serialization as well as drawing the GUI.
	/// </remarks>
	internal abstract class Setting<T> : ISetting
	{
		public event Action<T> ValueChanged;

		protected readonly string Key;
		protected readonly T DefaultValue;

		private T cachedValue;
		private bool cacheInitialized;

		private readonly GUIContent label;

		protected Setting(string key, T defaultValue = default)
		{
			Key = key;
			DefaultValue = defaultValue;
			label = new GUIContent(ObjectNames.NicifyVariableName(key));
			UserPrefs.Register(this);
		}

		public virtual T Value
		{
			get
			{
				if (cacheInitialized == false)
				{
					cachedValue = LoadValue();
					cacheInitialized = true;
				}
				return cachedValue;
			}
			protected set
			{
				SaveValue(value);
				cachedValue = value;
				cacheInitialized = true;

				ValueChanged?.Invoke(value);
			}
		}

		public static implicit operator T(Setting<T> pref)
		{
			return pref.Value;
		}

		public void DrawProperty()
		{
			EditorGUI.BeginChangeCheck();
			T newValue = DrawProperty(label, Value);
			if (EditorGUI.EndChangeCheck())
				Value = newValue;

			Rect lastRect = GUILayoutUtility.GetLastRect();
			HandleContextClick(lastRect);
		}

		protected virtual void HandleContextClick(Rect propertyRect)
		{
			Event current = Event.current;
			if (current.isMouse && current.button == 1 && propertyRect.Contains(current.mousePosition))
			{
				current.Use();
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Reset"), false, Reset);
				menu.ShowAsContext();
			}
		}

		protected abstract T DrawProperty(GUIContent label, T value);

		protected abstract T LoadValue();

		protected abstract void SaveValue(T value);

		public virtual void Reset()
		{
			Value = DefaultValue;
		}
	}
}
