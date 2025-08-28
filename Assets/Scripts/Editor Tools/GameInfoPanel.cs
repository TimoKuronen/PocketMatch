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
        if (!Services.Get<ISceneLoader>().IsGameScene())
            return;

        if (EditorApplication.isPlaying)
            Subscribe();
    }

    private void Subscribe()
    {
        if (subscribedToEvents)
            return;

        GridController.Instance.ActionTaken += OnMovesChanged;
        subscribedToEvents = true;
    }

    private void OnGUI()
    {
        if (!Services.Get<ISceneLoader>().IsGameScene())
            return;

        GUILayout.Label("Game Info", EditorStyles.boldLabel);

        if (!EditorApplication.isPlaying)
        {
            subscribedToEvents = false;
            return;
        }

        Subscribe();
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

    private void OnMovesChanged()
    {
        Repaint();
    }

    private void DisplayGameInfo()
    {
        GUILayout.Space(10);

        string movesLeft = Services.Get<ILevelManager>().MovesRemaining.ToString();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Moves left: ");
        EditorGUILayout.TextField(movesLeft);
        EditorGUILayout.EndHorizontal();

        string matchesLeft = GridController.Instance.MatchFinder.AvailableMatchesCount.ToString();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Matches left: ");
        EditorGUILayout.TextField(matchesLeft);
        EditorGUILayout.EndHorizontal();
    }

    private void OnDisable()
    {
        if (subscribedToEvents)
        {
            GridController.Instance.ActionTaken -= OnMovesChanged;
            subscribedToEvents = false;
        }
    }
}
#endif