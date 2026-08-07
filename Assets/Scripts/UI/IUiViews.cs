using System;

public interface IMainMenuView
{
    event Action PlayClicked;
    event Action SettingsClicked;

    void SetCoinCount(int coins);
    void SetVersion(string versionText);
}

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

public interface IMainMenuSettingsView
{
    event Action CloseClicked;
    event Action<float> SfxVolumeChanged;

    void SetSfxVolume(float value);
    void SetVersion(string versionText);
}

public interface IPauseSettingsView
{
    event Action CloseClicked;
    event Action RetryClicked;
    event Action MenuClicked;
    event Action<float> SfxVolumeChanged;

    void SetSfxVolume(float value);
    void SetVersion(string versionText);
}

public interface IWinView
{
    event Action NextLevelClicked;
    event Action MainMenuClicked;

    void SetEarnedCoins(int coins);
    void SetNextLevelButtonVisible(bool isVisible);
}

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

public interface ILevelSelectView
{
    event Action<int> LevelSelected;
    event Action BackClicked;
    event Action DisplayRequested;

    void BindLevels(int totalLevels, int unlockedThroughIndex, int highlightedIndex);
}
