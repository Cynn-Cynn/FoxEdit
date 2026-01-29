using UnityEditor;
using UnityEngine;

namespace FoxEdit.EditorUtils
{
    internal abstract class EditorPrefField<T>
    {
        protected string _key;
        protected T _defaultValue;

        public EditorPrefField(string key, T defaultValue)
        {
            this._key = key;
            _defaultValue = defaultValue;
        }

        private bool _hasCachedValue = false;
        private T _cachedValue;
        public T Value
        {
            get
            {
                if (_hasCachedValue)
                    return _cachedValue;
                _cachedValue = GetEditorPrefValue();
                _hasCachedValue = true;

                return _cachedValue;
            }
            set
            {
                this._cachedValue = value;
                this._hasCachedValue = true;
                SetEditorPrefValue(value);
            }
        }

        public abstract void SetEditorPrefValue(T value);

        public abstract T GetEditorPrefValue();
    }

    internal class EditorPrefColor : EditorPrefField<Color>
    {
        public EditorPrefColor(string key, Color defaultColor) : base(key, defaultColor)
        {
        }

        public override Color GetEditorPrefValue()
        {
            string colorStr = EditorPrefs.GetString(_key, null);
            if (colorStr == null)
                return _defaultValue;
            if (ColorUtility.TryParseHtmlString(colorStr, out Color color))
                return color;
            return _defaultValue;
        }

        public override void SetEditorPrefValue(Color value)
        {
            EditorPrefs.SetString(_key, ColorUtility.ToHtmlStringRGBA(value));
        }
    }

    internal class EditorPrefBool : EditorPrefField<bool>
    {
        public EditorPrefBool(string key, bool defaultValue) : base(key, defaultValue)
        {
        }

        public override bool GetEditorPrefValue()
        {
            return EditorPrefs.GetBool(_key, _defaultValue);
        }

        public override void SetEditorPrefValue(bool value)
        {
            EditorPrefs.SetBool(_key, value);
        }
    }

    internal class EditorPrefFloat : EditorPrefField<float>
    {
        float? _min = null;
        float? _max = null;

        public EditorPrefFloat(string key, float defaultValue, float? min = null, float? max = null) : base(key, defaultValue)
        {
            _min = min;
            _max = max;
        }

        public override float GetEditorPrefValue()
        {
            return ClampValue(EditorPrefs.GetFloat(_key, _defaultValue));
        }

        public override void SetEditorPrefValue(float value)
        {
            EditorPrefs.SetFloat(_key, ClampValue(value));
        }

        private float ClampValue(float value)
        {
            if (_min.HasValue)
                value = Mathf.Max(_min.Value, value);
            if (_max.HasValue)
                value = Mathf.Min(_max.Value, value);
            return value;
        }
    }
}