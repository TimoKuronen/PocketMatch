using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// HUD component for gameplay - displays game information (moves, coins, victory conditions).
/// This is NOT a menu in the stack system, just a display overlay.
/// </summary>
public class UI_GameHUD : MonoBehaviour, IDisposable
{
    #region Fields

    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;
    [SerializeField] private Transform victoryConditionsContainer;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI puzzleIndexText;
    [SerializeField] private UI_SettingsMenu settingsMenu;

    private MapData mapData;
    private List<VictoryConditionUI> victoryConditions = new List<VictoryConditionUI>();

    private ILevelManager levelManager;
    private IGameSessionService gameSessionService;
    private IScoreService scoreService;
    private ISaveService saveService;
    private MenuStackManager menuStackManager;

    public static event Action OnCheatButtonClicked;

    #endregion

    #region Initialization

    [Inject]
    public void Construct(
        ILevelManager levelManager,
        IGameSessionService gameSessionService,
        IScoreService scoreService,
        ISaveService saveService,
        MenuStackManager menuStackManager)
    {
        this.levelManager = levelManager;
        this.gameSessionService = gameSessionService;
        this.scoreService = scoreService;
        this.saveService = saveService;
        this.menuStackManager = menuStackManager;
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

    public void SettingsButtonPressed()
    {
        if (menuStackManager != null && settingsMenu != null)
        {
            if (menuStackManager.CanOpenMenu())
            {
                menuStackManager.PushMenu(settingsMenu);
            }
            else
            {
                Debug.LogWarning("Cannot open settings menu - matches are being processed");
            }
        }
    }

    /// <summary>
    /// Helper method for panels to load main menu (with confirmation handled by panels)
    /// </summary>
    public void LoadMainMenu()
    {
        if (menuStackManager != null)
        {
            menuStackManager.ClearStack();
        }
        Loader.Load(Loader.GameScene.MainMenu);
    }

    /// <summary>
    /// Helper method for panels to restart the level
    /// </summary>
    public void RestartLevel()
    {
        Loader.Restart();
    }

    /// <summary>
    /// Helper method for panels to load next level
    /// </summary>
    public void LoadNextLevel()
    {
        // This will be called from UI_WinPanel
        // We need adsService for this, so panels will handle it directly
    }

    public void Dispose()
    {
        GameSignals.OnSessionLoaded -= InitializeAfterSessionLoaded;
        if (levelManager != null)
        {
            levelManager.OnVictoryConditionsUpdated -= HandleVictoryConditionUpdate;
            levelManager.OnLevelWon -= OnLevelWon;
            levelManager.OnLevelLost -= OnLevelLost;
        }
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
        HideAllVictoryConditions();
        UpdateCoinCountText(scoreService.GetTotalScore());
        movesText.gameObject.SetActive(false);
        
        // Push win panel onto menu stack
        var winPanel = FindFirstObjectByType<UI_WinPanel>();
        if (winPanel != null && menuStackManager != null)
        {
            menuStackManager.PushMenu(winPanel);
        }
    }

    private void OnLevelLost()
    {
        HideAllVictoryConditions();
        movesText.gameObject.SetActive(false);
        
        // Push lose panel onto menu stack
        var losePanel = FindFirstObjectByType<UI_LosePanel>();
        if (losePanel != null && menuStackManager != null)
        {
            menuStackManager.PushMenu(losePanel);
        }
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
