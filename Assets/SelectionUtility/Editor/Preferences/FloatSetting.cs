namespace Xarbrough.SelectionUtility
{
    using UnityEditor;
    using UnityEngine;

    internal class FloatSetting : Setting<float>
    {
        private readonly bool clamp;
        private readonly float minValue;
        private readonly float maxValue;

        public FloatSetting(string key, float defaultValue, float min, float max) : base(key, defaultValue)
        {
            minValue = min;
            maxValue = max;
            clamp = true;
        }

        protected override float DrawProperty(GUIContent label, float value)
        {
            return EditorGUILayout.FloatField(label, value);
        }

        public override float Value
        {
            get => base.Value;
            protected set
            {
                if (clamp)
                    value = Mathf.Clamp(value, minValue, maxValue);

                base.Value = value;
            }
        }

        protected override float LoadValue()
        {
            return EditorPrefs.GetFloat(Key, DefaultValue);
        }

        protected override void SaveValue(float value)
        {
            EditorPrefs.SetFloat(Key, value);
        }
    }
}