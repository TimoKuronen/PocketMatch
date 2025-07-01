#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorShortcuts : EditorWindow
{
    private const string PrefabKey = "SelectedPrefabGUID";

    [SerializeField] private string itemText = string.Empty;
    [SerializeField] private string commandText = string.Empty;
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    [MenuItem("Limekicker/Editor Shortcuts")]
    public static void ShowWindow()
    {
        GetWindow<EditorShortcuts>("Editor Shortcuts");
    }

    private void OnEnable()
    {
        prefabs[PrefabKey] = null;
        LoadPrefab(PrefabKey);
    }

    private void OnGUI()
    {
        GUILayout.Label("Editor shortcuts", EditorStyles.boldLabel);

        DisplayButtons();
    }

    private void DisplayButtons()
    {
        //if (GUILayout.Button("Shuffle"))
        //{
        //    GridController.Instance.ShuffleBoard();
        //}
        //if (GUILayout.Button("Get matches"))
        //{
        //    GridController.Instance.StartMatchCycle();
        //}

        EditorGUILayout.Space();
        DrawPrefabField("Prefab", PrefabKey);
    }

    private void LoadPrefab(string key)
    {
        string savedGuid = EditorPrefs.GetString(key, string.Empty);
        if (!string.IsNullOrEmpty(savedGuid))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(savedGuid);
            prefabs[key] = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }
    }

    private void SavePrefab(string key)
    {
        if (prefabs[key] != null)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefabs[key]));
            EditorPrefs.SetString(key, guid);
        }
        else
        {
            EditorPrefs.DeleteKey(key);
        }
    }

    private void DrawPrefabField(string label, string key)
    {
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(label, prefabs[key], typeof(GameObject), false);
        if (newPrefab != prefabs[key])
        {
            prefabs[key] = newPrefab;
            SavePrefab(key);
        }
    }
}
#endif