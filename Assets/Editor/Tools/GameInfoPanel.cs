#if UNITY_EDITOR
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

    private void OnEnable()
    {
        if (EditorApplication.isPlaying)
            Subscribe();
    }

    private void Subscribe()
    {
        if (subscribedToEvents || !Loader.IsGameScene())
            return;

        var gridController = FindFirstObjectByType<GridController>();
        if (gridController != null)
        {
            gridController.ActionTaken += OnBoardChanged;
            subscribedToEvents = true;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Info", EditorStyles.boldLabel);

        if (!EditorApplication.isPlaying)
        {
            subscribedToEvents = false;
            return;
        }

        Subscribe();

        if (!Loader.IsGameScene())
        {
            return;
        }
        DisplayGameInfo();
    }

    void Update()
    {
        if (!EditorApplication.isPlaying)
            return;

        RefreshTimer();
    }

    float timer;
    private void RefreshTimer()
    {
        timer += Time.deltaTime;
        if (timer > 1)
        {
            timer = 0;
            RepaintGUI();
        }
    }

    void RepaintGUI()
    {
        Repaint();
    }

    private void OnBoardChanged()
    {
        Repaint();
    }

    private void DisplayGameInfo()
    {
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Timescale: ");
        EditorGUILayout.TextField(Time.timeScale.ToString());
        EditorGUILayout.EndHorizontal();

        var gridController = FindFirstObjectByType<GridController>();
        if (gridController == null || gridController.MatchFinder == null)
        {
            return;
        }

        string matchesLeft = gridController.BoardEvaluator.CountPotentialMoves().TotalMoves.ToString();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Matches left: ");
        EditorGUILayout.TextField(matchesLeft);
        EditorGUILayout.EndHorizontal();
    }

    private void OnDisable()
    {
        if (subscribedToEvents)
        {
            var gridController = FindFirstObjectByType<GridController>();
            if (gridController != null)
            {
                gridController.ActionTaken -= OnBoardChanged;
            }
            subscribedToEvents = false;
        }
    }
}
#endif