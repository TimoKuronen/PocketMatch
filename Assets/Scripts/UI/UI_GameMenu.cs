using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// Main UI menu for gameplay, handling victory conditions display, moves counter, and level completion states.
/// </summary>
public class UI_GameMenu : UIMenu, IDisposable
{
    #region Fields

    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject nextLevelButton;

    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;
    [SerializeField] private Transform victoryConditionsContainer;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI puzzleIndexText;

    private MapData mapData;
    private List<VictoryConditionUI> victoryConditions = new List<VictoryConditionUI>();

    private ILevelManager levelManager;
    private IAdsService adsService;
    private IGameSessionService gameSessionService;
    private IScoreService scoreService;
    private ISaveService saveService;

    public static event Action OnCheatButtonClicked;

    #endregion

    #region Initialization

    [Inject]
    public void Construct(
        ILevelManager levelManager,
        IAdsService adsService,
        IGameSessionService gameSessionService,
        IScoreService scoreService,
        ISaveService saveService)
    {
        this.levelManager = levelManager;
        this.adsService = adsService;
        this.gameSessionService = gameSessionService;
        this.scoreService = scoreService;
        this.saveService = saveService;
    }

    public void Start()
    {       
        // If session is already loaded, initialize immediately
        if (GameSignals.IsSessionLoaded)
        {
            InitializeAfterSessionLoaded();
        }
        else GameSignals.OnSessionLoaded += InitializeAfterSessionLoaded;
    }

    private void InitializeAfterSessionLoaded()
    {
        mapData = gameSessionService.CurrentMapData;
        UpdatePuzzleIndexText(saveService.PlayerData.nextLevelIndex + 1);

        levelManager.OnVictoryConditionsUpdated += HandleVictoryConditionUpdate;
        levelManager.OnLevelWon += OnLevelWon;
        levelManager.OnLevelLost += OnLevelLost;

        LoadVictoryConditions();
    }

    #endregion

    #region Public Methods

    public void CheatWinButtonPressed()
    {
        OnCheatButtonClicked?.Invoke();
    }

    public void MenuButtonPressed()
    {
        Loader.Load(Loader.GameScene.MainMenu);
    }

    public void RestartButtonPressed()
    {
        Loader.Restart();
    }

    public void NextLevelButtonPressed()
    {
        Loader.ShowInterstitialThenContinue(adsService, Loader.GameScene.PlayScene);
    }

    public void Dispose()
    {
        GameSignals.OnSessionLoaded -= InitializeAfterSessionLoaded;
        levelManager.OnVictoryConditionsUpdated -= HandleVictoryConditionUpdate;
        levelManager.OnLevelWon -= OnLevelWon;
        levelManager.OnLevelLost -= OnLevelLost;
    }

    #endregion

    #region Event Handlers

    private void HandleVictoryConditionUpdate()
    {
        UpdateMovesText(levelManager.MovesRemaining);

        // Update each victory condition UI with current progress
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

    private void OnLevelWon()
    {
        winPanel.SetActive(true);
        HideAllVictoryConditions();

        if (gameSessionService.IsLevelCapReached)
        {
            nextLevelButton.SetActive(false);
        }

        UpdateCoinCountText(scoreService.GetTotalScore());
        movesText.gameObject.SetActive(false);
    }

    private void OnLevelLost()
    {
        losePanel.SetActive(true);
        HideAllVictoryConditions();
        movesText.gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    private void LoadVictoryConditions()
    {
        UpdateMovesText(mapData.VictoryConditions.MoveLimit);

        // Create UI elements for each required color match condition
        foreach (var item in mapData.VictoryConditions.RequiredColorMatchCount)
        {
            CreateColorMatchCondition(item.TileColor, item.TileCount);
        }

        // Create UI element for destroyable tiles condition if required
        if (mapData.VictoryConditions.DestroyableTileCount > 0)
        {
            CreateDestroyableTileCondition(mapData.VictoryConditions.DestroyableTileCount);
        }
    }

    #endregion

    #region Helper Methods

    private void UpdatePuzzleIndexText(int levelIndex)
    {
        var sb = new StringBuilder();
        sb.Append("Puzzle #");
        sb.Append(levelIndex.ToString());
        puzzleIndexText.text = sb.ToString();
    }

    private void UpdateMovesText(int moves)
    {
        var sb = new StringBuilder();
        sb.Append("Moves: ");
        sb.Append(moves.ToString());
        movesText.text = sb.ToString();
    }

    private void UpdateCoinCountText(int coins)
    {
        var sb = new StringBuilder();
        sb.Append("x ");
        sb.Append(coins.ToString());
        coinCountText.text = sb.ToString();
    }

    private void HideAllVictoryConditions()
    {
        foreach (var item in victoryConditions)
        {
            item.gameObject.SetActive(false);
        }
    }

    private void CreateColorMatchCondition(TileType tileColor, int tileCount)
    {
        var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);
        victoryCondition.Init(
            tileCount.ToString(),
            tileIconCollection.GetIcon(tileColor, TilePower.None, TileState.Normal),
            tileColor,
            ConditionType.ColorMatch);
        victoryConditions.Add(victoryCondition);
    }

    private void CreateDestroyableTileCondition(int destroyableTileCount)
    {
        var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);
        victoryCondition.Init(
            destroyableTileCount.ToString(),
            tileIconCollection.GetIcon(TileType.Red, TilePower.None, TileState.Destroyable),
            TileType.Red,
            ConditionType.DestroyableTiles);
        victoryConditions.Add(victoryCondition);
    }

    #endregion
}
