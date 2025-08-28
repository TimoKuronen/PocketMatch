using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : UIMenu
{
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;
    [SerializeField] private Transform victoryConditionsContainer;
    [SerializeField] private ColorPalette colorPalette;
    [SerializeField] private TextMeshProUGUI movesText;

    private MapData mapData;
    private List<VictoryConditionUI> victoryConditions = new List<VictoryConditionUI>();
    private ILevelManager levelManager;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        mapData = Services.Get<IGameSessionService>().CurrentMapData;

        levelManager = Services.Get<ILevelManager>();
        levelManager.VictoryConditionsUpdated += OnVictoryConditionsUpdated;
        levelManager.LevelWon += OnLevelWon;
        levelManager.LevelLost += OnLevelLost;

        LoadVictoryConditions();
    }

    private void LoadVictoryConditions()
    {
        movesText.text = "Moves: " + mapData.VictoryConditions.MoveLimit.ToString();

        foreach (var item in mapData.VictoryConditions.RequiredColorMatchCount)
        {
            var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);

            victoryCondition.Init(
                item.TileCount.ToString(),
                tileIconCollection.GetIcon(item.TileColor, TilePower.None, TileState.Normal),
                item.TileColor,
                colorPalette,
                ConditionType.ColorMatch);

            victoryConditions.Add(victoryCondition);
        }

        if (mapData.VictoryConditions.DestroyableTileCount > 0)
        {
            var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);

            victoryCondition.Init(
                mapData.VictoryConditions.DestroyableTileCount.ToString(),
                tileIconCollection.GetIcon(TileType.Red, TilePower.None, TileState.Destroyable),
                TileType.Red,
                colorPalette,
                ConditionType.DestroyableTiles);

            victoryConditions.Add(victoryCondition);
        }
    }

    private void OnVictoryConditionsUpdated(LevelManager levelManager)
    {
        movesText.text = "Moves: " + levelManager.MovesRemaining.ToString();

        foreach (var item in victoryConditions)
        {
            if (item.ConditionType == ConditionType.ColorMatch)
            {
                foreach (var condition in levelManager.VictoryConditions.RequiredColorMatchCount)
                {
                    item.UpdateUI(condition.TileCount.ToString());
                }
            }
            else if (item.ConditionType == ConditionType.DestroyableTiles)
            {
                item.UpdateUI(levelManager.VictoryConditions.DestroyableTileCount.ToString());
            }
        }
    }

    public void MenuButtonPressed()
    {

    }

    public void RestartButtonPressed()
    {

    }

    public void NextLevelButtonPressed()
    {

    }

    private void OnLevelWon()
    {
        winPanel.SetActive(true);

        foreach (var item in victoryConditions)
        {
            item.gameObject.SetActive(false);
        }

        movesText.gameObject.SetActive(false);
    }

    private void OnLevelLost()
    {
        losePanel.SetActive(true);

        foreach (var item in victoryConditions)
        {
            item.gameObject.SetActive(false);
        }

        movesText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        levelManager.VictoryConditionsUpdated -= OnVictoryConditionsUpdated;
        levelManager.LevelWon -= OnLevelWon;
        levelManager.LevelLost -= OnLevelLost;
    }
}