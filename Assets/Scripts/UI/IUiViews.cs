using System;

/// <summary>MVP view contract for the main menu screen.</summary>
public interface IMainMenuView
{
    event Action PlayClicked;
    event Action SettingsClicked;

    void SetCoinCount(int coins);
    void SetVersion(string versionText);
}

/// <summary>MVP view contract for in-level HUD: moves, wallet, and victory progress.</summary>
public interface IGameHudView
{
    event Action SettingsClicked;

    void SetMoves(int moves);
    void SetWalletBalance(int balance);
    void SetLevelIndex(int levelIndex);
    void InitializeVictoryConditions(VictoryConditions victoryConditions);
    void UpdateVictoryConditions(VictoryConditions victoryConditions, int movesRemaining);
    void HideVictoryConditions();
    void ShowVictoryConditions();
}

/// <summary>MVP view contract for main-menu audio settings.</summary>
public interface IMainMenuSettingsView
{
    event Action CloseClicked;
    event Action<float> SfxVolumeChanged;

    void SetSfxVolume(float value);
    void SetVersion(string versionText);
}

/// <summary>MVP view contract for pause-menu settings and level exit actions.</summary>
public interface IPauseSettingsView
{
    event Action CloseClicked;
    event Action RetryClicked;
    event Action MenuClicked;
    event Action<float> SfxVolumeChanged;

    void SetSfxVolume(float value);
    void SetVersion(string versionText);
}

/// <summary>MVP view contract for the level-complete overlay.</summary>
public interface IWinView
{
    event Action NextLevelClicked;
    event Action MainMenuClicked;

    void SetEarnedCoins(int coins);
    void SetNextLevelButtonVisible(bool isVisible);
}

/// <summary>MVP view contract for the level-failed overlay and continue offers.</summary>
public interface ILoseView
{
    event Action RestartClicked;
    event Action MainMenuClicked;
    event Action ContinueWithCoinsClicked;
    event Action ContinueWithAdClicked;
    event Action Opened;

    void SetWalletBalance(int balance);
    void SetContinueWithCoinsAvailable(bool isAvailable);
    void SetContinueWithAdAvailable(bool isAvailable);
}

/// <summary>MVP view contract for the level-select grid.</summary>
public interface ILevelSelectView
{
    event Action<int> LevelSelected;
    event Action BackClicked;
    event Action DisplayRequested;

    void BindLevels(int totalLevels, int unlockedThroughIndex, int highlightedIndex);
}
