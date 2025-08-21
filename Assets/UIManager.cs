using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;

    private MapData mapData;
    private List<VictoryConditionUI> victoryConditions = new List<VictoryConditionUI>();

    IEnumerator Start()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        mapData = Services.Get<IGameSessionService>().CurrentMapData;

        LoadVictoryConditions();

        Services.Get<ILevelManager>().VictoryConditionsUpdated += OnVictoryConditionsUpdated;
    }

    private void LoadVictoryConditions()
    {
        foreach (var item in mapData.VictoryConditions.RequiredColorMatchCount)
        {
            var victoryCondition = Instantiate(victoryConditionPrefab, transform);
            //victoryCondition.GetComponent<VictoryConditionUI>().Init();
            victoryConditions.Add(victoryCondition);
        }
        for (int i = 0; i < mapData.VictoryConditions.DestroyableTileCount; i++)
        {
            var victoryCondition = Instantiate(victoryConditionPrefab, transform);
            //victoryCondition.GetComponent<VictoryConditionUI>().Init();
            victoryConditions.Add(victoryCondition);
        }
    }

    private void OnVictoryConditionsUpdated(LevelManager levelManager)
    {
        
    }

    private void OnDestroy()
    {
        if (Services.Get<ILevelManager>() != null)
        {
            Services.Get<ILevelManager>().VictoryConditionsUpdated -= OnVictoryConditionsUpdated;
        }
    }
}