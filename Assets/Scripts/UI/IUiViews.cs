using System;

public interface IMainMenuView
{
    event Action PlayClicked;
    event Action SettingsClicked;
    event Action ResetSaveClicked;

    void SetCoinCount(int coins);
    void SetLevelIndex(int levelIndex);
    void SetVersion(string versionText);
}

public interface IGameHudView
{
    event Action SettingsClicked;
    event Action CheatWinClicked;

    void SetMoves(int moves);
    void SetCoinCount(int coins);
    void SetLevelIndex(int levelIndex);
    void InitializeVictoryConditions(VictoryConditions victoryConditions);
    void UpdateVictoryConditions(VictoryConditions victoryConditions, int movesRemaining);
    void HideVictoryConditions();
}

public interface ISettingsView
{
    event Action CloseClicked;
    event Action RetryClicked;
    event Action MenuClicked;
    event Action<float> SfxVolumeChanged;

    void ConfigureForContext(SettingsPanel.SettingsContext context);
    void SetSfxVolume(float value);
}

public interface IWinView
{
    event Action NextLevelClicked;
    event Action MainMenuClicked;

    void SetCoinCount(int coins);
    void SetNextLevelButtonVisible(bool isVisible);
}

public interface ILoseView
{
    event Action RestartClicked;
    event Action MainMenuClicked;
}

public interface ILevelSelectView
{
    event Action<int> LevelSelected;
    event Action BackClicked;
}

