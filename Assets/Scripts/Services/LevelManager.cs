using System;
using System.Collections;
using UnityEngine;

public class LevelManager : ILevelManager
{
    public int MovesRemaining { get; private set; }
    public MapData LocalMapData { get; private set; }
    public VictoryConditions VictoryConditions { get; private set; }
    public Action<LevelManager> VictoryConditionsUpdated { get; set; }

    private ISaveService saveService;

    public void Initialize()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(SetLevelData());
        saveService = Services.Get<ISaveService>();
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

        MovesRemaining = LocalMapData.VictoryConditions.MoveLimit;

        Debug.Log($"LevelManager {LocalMapData.name} initialized with MoveLimit: {MovesRemaining}");
        VictoryConditions = LocalMapData.VictoryConditions;

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
            VictoryConditions.DestroyableTileCount--;
        }
        else if (VictoryConditions.RequiredColorMatchCount != null && VictoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in VictoryConditions.RequiredColorMatchCount)
            {
                if (data.Type == match.TileColor)
                {
                    match.TileCount--;

                    if (match.TileCount < 0) 
                        match.TileCount = 0;
                }
            }
        }

        VictoryConditionsUpdated?.Invoke(this);
    }

    private void CheckVictoryConditions(TileData[,] obj)
    {
        if (!AreVictoryConditionsMet())
        {
            if (MovesRemaining <= 0)
            {
                ToggleLoseEvent();
            }
        }
        else
        {
            ToggleWinEvent();
        }
    }

    private bool AreVictoryConditionsMet()
    {
        // Check if all required colors have been matched
        if (VictoryConditions.RequiredColorMatchCount != null && VictoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in VictoryConditions.RequiredColorMatchCount)
            {
                if (match.TileCount > 0)
                {
                    Debug.Log($"Victory conditions to destroy colors, still required : {match.TileCount} ");
                    return false;
                }
                else Debug.Log($"Victory condition met for color: {match.TileColor}");
            }
        }
        // Check if all the required destroyable tiles have been cleared
        if (VictoryConditions.DestroyableTileCount > 0)
        {
            Debug.Log("Victory condition not met: Destroyable tiles remaining " + VictoryConditions.DestroyableTileCount);
            return false; 
        }
        else Debug.Log("All destroyable tiles cleared.");

        return true;
    }

    private void OnActionTaken()
    {
        MovesRemaining--;
        VictoryConditionsUpdated?.Invoke(this);
    }

    private void ToggleWinEvent()
    {
        saveService.PlayerData.nextLevelIndex++;
        saveService.Save();
    }

    private void ToggleLoseEvent()
    {

    }

    public void Dispose()
    {
        GridController.Instance.ActionTaken -= OnActionTaken;
        GridController.Instance.BoardUpdated -= CheckVictoryConditions;
        GridController.Instance.TileDestroyed -= OnTileDestroyed;
    }
}