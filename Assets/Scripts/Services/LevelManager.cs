using System;
using System.Collections;
using UnityEngine;

public class LevelManager : ILevelManager
{
    public MapData LocalMapData { get; private set; }

    public int MovesRemaining { get; private set; }

    private VictoryConditions victoryConditions;

    public void Initialize()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(SetLevelData());
    }

    private IEnumerator SetLevelData()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        LocalMapData = MonoBehaviour.Instantiate(Services.Get<IGameSessionService>().CurrentMapData);

        if (LocalMapData == null)
        {
            Debug.LogError("MapData not assigned.");
            yield break;
        }

        MovesRemaining = LocalMapData.MoveLimit;
        victoryConditions = LocalMapData.VictoryConditions;

        yield return new WaitUntil(() => GridController.Instance != null);

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        GridController.Instance.ActionTaken += OnActionTaken;
        GridController.Instance.BoardUpdated += CheckVictoryConditions;
        GridController.Instance.TileDestroyed += OnTileDestroyed;
    }

    private void OnTileDestroyed(TileData data)
    {
        if (data.State == TileState.Destroyable)
        {
            victoryConditions.DestroyableTileCount--;
        }
        else if (victoryConditions.RequiredColorMatchCount != null && victoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in victoryConditions.RequiredColorMatchCount)
            {
                if (data.Type == match.TileColor)
                {
                    match.TileCount--;
                }
            }
        }
    }

    private void CheckVictoryConditions(TileData[,] obj)
    {
        // Check if all required colors have been matched
        if (victoryConditions.RequiredColorMatchCount != null && victoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in victoryConditions.RequiredColorMatchCount)
            {
                if (match.TileCount > 0)
                {
                    Debug.Log($"Victory condition not met for color: {match.TileColor}");
                    return; // Not all required colors matched
                }
                else Debug.Log($"Victory condition met for color: {match.TileColor}");
            }
        }
        // Check if all the required destroyable tiles have been cleared
        if (victoryConditions.DestroyableTileCount > 0)
        {
            Debug.Log("Victory condition not met: Destroyable tiles remaining.");
            return; // Not all destroyable tiles cleared
        }
        else Debug.Log("All destroyable tiles cleared.");

        // All checked conditions are met, toggle victory
    }

    private void OnActionTaken()
    {
        MovesRemaining--;

    }

    void ToggleWinEvent()
    {

    }

    void ToggleLoseEvent()
    {

    }

    public void Dispose()
    {
        GridController.Instance.ActionTaken -= OnActionTaken;
        GridController.Instance.BoardUpdated -= CheckVictoryConditions;
    }
}
