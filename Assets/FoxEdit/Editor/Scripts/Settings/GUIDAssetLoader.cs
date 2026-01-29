using UnityEditor;
using UnityEngine;

internal class GUIDAssetLoader<T> where T : Object
{
    private T _asset;
    public T Asset
    {
        get
        {
            if (_asset != null)
                return _asset;
            _asset = FindAsset();
            return _asset;
        }
    }
    private string _guid = null;

    public GUIDAssetLoader(string guid)
    {
        _guid = guid;
    }

    private T FindAsset()
    {
        string path = AssetDatabase.GUIDToAssetPath(_guid);
        
        if (!string.IsNullOrEmpty(path))
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return null;
    }
}
