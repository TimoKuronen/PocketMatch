using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

public class UI_GameMenu : UIMenu
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject nextLevelButton;

    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;
    [SerializeField] private Transform victoryConditionsContainer;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI coinCountText;

    private MapData mapData;
    private List<VictoryConditionUI> victoryConditions = new List<VictoryConditionUI>();

    private ILevelManager levelManager;
    private IAdsService adsService;
    private IGameSessionService gameSessionService;
    private IScoreService scoreService;

    [Inject]    
    public void Construct(
        ILevelManager levelManager, 
        IAdsService adsService, 
        IGameSessionService gameSessionService,
        IScoreService scoreService)
    {
        Debug.Log("UI_GameMenu Construct called with services " + levelManager + ", " + adsService + ", " + gameSessionService + ", " + scoreService);
        this.levelManager = levelManager;
        this.adsService = adsService;
        this.gameSessionService = gameSessionService;
        this.scoreService = scoreService;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameSignals.IsSessionLoaded);

        mapData = gameSessionService.CurrentMapData;

        levelManager.OnVictoryConditionsUpdated += OnVictoryConditionsUpdated;
        levelManager.OnLevelWon += OnLevelWon;
        levelManager.OnLevelLost += OnLevelLost;

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
                    if (item.TileType == condition.TileColor)
                    {
                        item.UpdateUI(condition.TileCount.ToString());
                    }
                }
            }
            else if (item.ConditionType == ConditionType.DestroyableTiles)
            {
                item.UpdateUI(levelManager.VictoryConditions.DestroyableTileCount.ToString());
            }
        }
    }

    /// <summary>
    /// Called via UI Button
    /// </summary>
    public void MenuButtonPressed()
    {
        Debug.Log("Menu button pressed");
        Loader.Load(Loader.GameScene.MainMenu);
    }

    /// <summary>
    /// Called via UI Button
    /// </summary>
    public void RestartButtonPressed()
    {
        Debug.Log("Restart button pressed");
        Loader.Restart();
    }

    /// <summary>
    /// Called via UI Button
    /// </summary>
    public void NextLevelButtonPressed()
    {
        Debug.Log("Next level button pressed");
        StartCoroutine(HandleLevelLoadingWithAd());
    }

    private IEnumerator HandleLevelLoadingWithAd()
    {
//#if UNITY_EDITOR

        StartCoroutine(Loader.ShowInterstitialThenContinue(adsService, Loader.GameScene.PlayScene));
//#endif
        Debug.Log("Waiting for ad to complete...");
        yield return new WaitUntil(() => adsService.InterstitialAdCompleted);
        Loader.Load(Loader.GameScene.PlayScene);
        Debug.Log("Ad completed, loading next level...");
    }

    private void OnLevelWon()
    {
        winPanel.SetActive(true);

        foreach (var item in victoryConditions)
        {
            item.gameObject.SetActive(false);
        }

        if (gameSessionService.IsLevelCapReached)
        {
            nextLevelButton.SetActive(false);
        }

        coinCountText.text = "x " + scoreService.GetTotalScore().ToString();
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

    public void Dispose()
    {
        levelManager.OnVictoryConditionsUpdated -= OnVictoryConditionsUpdated;
        levelManager.OnLevelWon -= OnLevelWon;
        levelManager.OnLevelLost -= OnLevelLost;
    }
}