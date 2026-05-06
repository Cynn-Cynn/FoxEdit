using System;
using System.IO;
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

    internal class EditorPrefVector3 : EditorPrefField<Vector3>
    {
        public EditorPrefVector3(string key, Vector3 defaultValue) : base(key, defaultValue)
        {
        }

        public override Vector3 GetEditorPrefValue()
        {
            string vectorStr = EditorPrefs.GetString(_key, null);
            if (vectorStr == null)
                return _defaultValue;
            string[] splitted = vectorStr.Split(',');

            if (splitted.Length != 3)
                return _defaultValue;
            float[] floats = new float[3];

            for (int i = 0; i < 3; i++)
            {
                if (!float.TryParse(splitted[i], out floats[i]))
                    return _defaultValue;
            }

            return new Vector3(floats[0], floats[1], floats[2]);
        }

        public override void SetEditorPrefValue(Vector3 value)
        {
            EditorPrefs.SetString(_key, string.Format("{0},{1},{2}", value.x, value.y, value.z));
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

    internal class EditorPrefString : EditorPrefField<string>
    {
        public EditorPrefString(string key, string defaultValue) : base(key, defaultValue)
        {
        }

        public override string GetEditorPrefValue()
        {
            return EditorPrefs.GetString(_key, _defaultValue);
        }

        public override void SetEditorPrefValue(string value)
        {
            EditorPrefs.SetString(_key, value);
        }
    }
}