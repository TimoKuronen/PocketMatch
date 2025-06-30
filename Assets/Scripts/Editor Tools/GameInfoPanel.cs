#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameInfoPanel : EditorWindow
{
    bool subscribedToEvents = false;

    [MenuItem("Limekicker/GameInfoPanel")]
    public static void ShowWindow()
    {
        GetWindow<GameInfoPanel>("Game Info Panel");
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Info", EditorStyles.boldLabel);

        if (!EditorApplication.isPlaying)
        {
            subscribedToEvents = false;
            return;
        }

        Repaint();

        DisplayGameInfo();
    }

    private void DisplayGameInfo()
    {
        GUILayout.Space(10);
        string autoshootStatus = "";// GridController.Instance.CurrentMatches.Count.ToString();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Match count on board: ");
        EditorGUILayout.TextField(autoshootStatus);
        EditorGUILayout.EndHorizontal();
    }
}
#endif