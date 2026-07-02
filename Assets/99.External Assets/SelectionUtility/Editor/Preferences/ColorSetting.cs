namespace Xarbrough.SelectionUtility
{
    using UnityEditor;
    using UnityEngine;

    internal class ColorSetting : Setting<Color>
    {
        public ColorSetting(string key, Color defaultValue = default) : base(key, defaultValue)
        {
        }

        protected override Color DrawProperty(GUIContent label, Color value)
        {
            return EditorGUILayout.ColorField(label, value);
        }

        protected override Color LoadValue()
        {
            string html = EditorPrefs.GetString(Key);
            
            if (html == string.Empty)
                return DefaultValue;

            return ColorUtility.TryParseHtmlString("#" + html, out Color color) ? color : Color.white;
        }

        protected override void SaveValue(Color value)
        {
            string html = ColorUtility.ToHtmlStringRGBA(value);
            EditorPrefs.SetString(Key, html);
        }
    }
}
