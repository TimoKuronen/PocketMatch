using System;
using System.Collections;
using System.Collections.Generic;
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
    }

    private IEnumerator SubscribeToEvents()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        yield return new WaitUntil(() => GridController.Instance != null);

        GridController.Instance.ActionTaken += OnActionTaken;
    }

    private void OnActionTaken()
    {
        LocalMapData.MoveLimit--;
    }

    public void Dispose()
    {
        GridController.Instance.ActionTaken -= OnActionTaken;
    }
}
