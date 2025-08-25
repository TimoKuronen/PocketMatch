using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;
    [SerializeField] private Transform victoryConditionsContainer;
    [SerializeField] private ColorPalette colorPalette;

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
            var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);

            victoryCondition.GetComponent<VictoryConditionUI>().Init(
                item.TileCount.ToString(),
                tileIconCollection.GetIcon(item.TileColor, TilePower.None, TileState.Normal),
                colorPalette);

            victoryConditions.Add(victoryCondition);
        }

        if (mapData.VictoryConditions.DestroyableTileCount > 0)
        {
            var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);

            victoryCondition.GetComponent<VictoryConditionUI>().Init(
                mapData.VictoryConditions.DestroyableTileCount.ToString(),
                tileIconCollection.GetIcon(TileType.Red, TilePower.None, TileState.Destroyable),
                colorPalette);

            victoryConditions.Add(victoryCondition);
        }
    }

    private void OnVictoryConditionsUpdated(LevelManager levelManager)
    {
        foreach (var item in victoryConditions)
        {

        }
    }

    private void OnDestroy()
    {
        if (Services.Get<ILevelManager>() != null)
        {
            Services.Get<ILevelManager>().VictoryConditionsUpdated -= OnVictoryConditionsUpdated;
        }
    }
}