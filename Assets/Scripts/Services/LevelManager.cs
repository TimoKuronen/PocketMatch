using System.Collections;
using UnityEngine;

public class LevelManager : ILevelManager
{
    public MapData LocalMapData { get; private set; }

    public int MovesRemaining { get; private set; }

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

        yield return new WaitUntil(() => GridController.Instance != null);

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        GridController.Instance.ActionTaken += OnActionTaken;
        GridController.Instance.BoardUpdated += CheckVictoryConditions;
    }

    private void CheckVictoryConditions(TileData[,] obj)
    {

    }

    private void OnActionTaken()
    {
        MovesRemaining--;
        Debug.Log($"Moves remaining: {MovesRemaining}");
    }

    private void CheckVictoryConditions()
    {
        if (MovesRemaining <= 0)
        {
            Debug.Log("Game Over: No moves remaining.");
            // Handle game over logic here
            return;
        }
    }

    public void Dispose()
    {
        GridController.Instance.ActionTaken -= OnActionTaken;
        GridController.Instance.BoardUpdated -= CheckVictoryConditions;
    }
}
