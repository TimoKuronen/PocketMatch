using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD component for gameplay - displays game information (moves, coins, victory conditions).
/// </summary>
public class UIGameHUD : MonoBehaviour, IGameHudView, IDisposable
{
    #region Fields

    [SerializeField] private TileIconCollection tileIconCollection;
    [SerializeField] private VictoryConditionUI victoryConditionPrefab;
    [SerializeField] private Transform victoryConditionsContainer;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject gameAreaRoot;

    private readonly List<VictoryConditionUI> victoryConditions = new List<VictoryConditionUI>();
    private readonly StringBuilder sb = new StringBuilder(32);

    public event Action SettingsClicked;

    #endregion

    #region Lifecycle

    public void Start()
    {
        settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
    }

    public void Dispose()
    {
        settingsButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region Public API

    public void SetMoves(int moves)
    {
        UpdateMovesText(moves);
    }

    public void SetWalletBalance(int balance)
    {
        if (coinCountText == null)
            return;

        sb.Clear();
        sb.Append("x ");
        sb.Append(balance);
        coinCountText.text = sb.ToString();
    }

    public void SetLevelIndex(int levelIndex)
    {
        UpdatePuzzleIndexText(levelIndex);
    }

    public void InitializeVictoryConditions(VictoryConditions victoryConditions)
    {
        // Clear existing UI instances
        foreach (var existing in victoryConditionsContainer.GetComponentsInChildren<VictoryConditionUI>())
        {
            Destroy(existing.gameObject);
        }
        this.victoryConditions.Clear();

        // Rebuild from provided data
        UpdateMovesText(victoryConditions.MoveLimit);

        if (victoryConditions.RequiredColorMatchCount != null)
        {
            foreach (var item in victoryConditions.RequiredColorMatchCount)
            {
                CreateColorMatchCondition(item.TileColor, item.TileCount);
            }
        }

        if (victoryConditions.DestroyableTileCount > 0)
        {
            CreateDestroyableTileCondition(victoryConditions.DestroyableTileCount);
        }
    }

    public void UpdateVictoryConditions(VictoryConditions victoryConditions, int movesRemaining)
    {
        UpdateMovesText(movesRemaining);
        foreach (var ui in victoryConditionsContainer.GetComponentsInChildren<VictoryConditionUI>())
        {
            if (ui.ConditionType == ConditionType.ColorMatch &&
                victoryConditions.RequiredColorMatchCount != null)
            {
                foreach (var condition in victoryConditions.RequiredColorMatchCount)
                {
                    if (ui.TileType == condition.TileColor)
                    {
                        ui.UpdateUI(condition.TileCount);
                        break;
                    }
                }
            }
            else if (ui.ConditionType == ConditionType.DestroyableTiles)
            {
                ui.UpdateUI(victoryConditions.DestroyableTileCount);
            }
        }
    }

    public void HideVictoryConditions()
    {
        HideAllVictoryConditions();
        if (movesText != null)
            movesText.gameObject.SetActive(false);
    }

    public void ShowVictoryConditions()
    {
        if (movesText != null)
            movesText.gameObject.SetActive(true);

        foreach (var item in victoryConditions)
        {
            if (item != null)
                item.gameObject.SetActive(true);
        }
    }

    public void SetGameAreaVisible(bool isVisible)
    {
        if (gameAreaRoot != null)
            gameAreaRoot.SetActive(isVisible);
    }

    #endregion

    #region Private Helpers

    private void UpdatePuzzleIndexText(int levelIndex)
    {
        sb.Clear();
        sb.Append("Puzzle #");
        sb.Append(levelIndex);
        currentLevelText.text = sb.ToString();
    }

    private void UpdateMovesText(int moves)
    {
        sb.Clear();
        sb.Append("Moves: ");
        sb.Append(moves);
        movesText.text = sb.ToString();
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
            tileCount,
            tileIconCollection.GetIcon(tileColor, TilePower.None, TileState.Normal),
            tileColor,
            ConditionType.ColorMatch);
        victoryConditions.Add(victoryCondition);
    }

    private void CreateDestroyableTileCondition(int destroyableTileCount)
    {
        var victoryCondition = Instantiate(victoryConditionPrefab, victoryConditionsContainer);
        victoryCondition.Init(
            destroyableTileCount,
            tileIconCollection.GetIcon(TileType.Red, TilePower.None, TileState.Destroyable),
            TileType.Red,
            ConditionType.DestroyableTiles);
        victoryConditions.Add(victoryCondition);
    }

    #endregion
}
