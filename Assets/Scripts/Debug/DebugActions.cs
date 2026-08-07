using UnityEngine;

public static class DebugActionIds
{
    public const string ForceWin = "force_win";
    public const string ForceLose = "force_lose";
    public const string ResetSave = "reset_save";
    public const string SetCoins = "set_coins";
    public const string AddCoins = "add_coins";
    public const string ToggleBoardLogging = "toggle_board_logging";
    public const string ShowBannerAd = "show_banner_ad";
    public const string ShowInterstitialAd = "show_interstitial_ad";
}

public sealed class ForceWinDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ForceWin;
    public string Category => "Level";
    public string Label => "Force Win";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.DebugTools.HasLevelTarget;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.DebugTools.ForceWin();
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class ForceLoseDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ForceLose;
    public string Category => "Level";
    public string Label => "Force Lose";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.DebugTools.HasLevelTarget;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.DebugTools.ForceLose();
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class ResetSaveDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ResetSave;
    public string Category => "Save";
    public string Label => "Reset Save";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => true;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.Save.ResetToDefaults();

        var settings = DebugToolsSettings.Load();
        if (settings.startingCoinsOverride > 0)
            context.Economy.SetBalance(settings.startingCoinsOverride);

        Debug.Log("[DebugTools] Save reset to defaults.");
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class SetCoinsDebugAction : IDebugAction
{
    public string Id => DebugActionIds.SetCoins;
    public string Category => "Economy";
    public string Label => "Set Coins";
    public DebugActionKind Kind => DebugActionKind.IntField;

    public bool IsAvailable(DebugContext context) => true;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.Economy.SetBalance(Mathf.Max(0, intValue));
        Debug.Log($"[DebugTools] Coins set to {Mathf.Max(0, intValue)}.");
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class AddCoinsDebugAction : IDebugAction
{
    public string Id => DebugActionIds.AddCoins;
    public string Category => "Economy";
    public string Label => "Add Coins";
    public DebugActionKind Kind => DebugActionKind.IntField;

    public bool IsAvailable(DebugContext context) => true;

    public void Execute(DebugContext context, int intValue = 0)
    {
        var amount = intValue > 0 ? intValue : 100;
        context.Economy.AddCoins(amount, "debug_tools");
        Debug.Log($"[DebugTools] Added {amount} coins.");
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class ToggleBoardLoggingDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ToggleBoardLogging;
    public string Category => "Board";
    public string Label => "Log To Debug File";
    public DebugActionKind Kind => DebugActionKind.Toggle;

    public bool IsAvailable(DebugContext context) => true;

    public void Execute(DebugContext context, int intValue = 0)
    {
        BoardDebugConfig.IsEnabled = intValue != 0;
    }

    public bool GetToggleState(DebugContext context) => BoardDebugConfig.IsEnabled;
}

public sealed class ShowBannerAdDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ShowBannerAd;
    public string Category => "Ads";
    public string Label => "Show Banner Ad";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.Ads != null && context.Ads.IsInitialized;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.Ads.ShowBannerAd();
        Debug.Log("[DebugTools] Banner ad show requested.");
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class ShowInterstitialAdDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ShowInterstitialAd;
    public string Category => "Ads";
    public string Label => "Show Interstitial Ad";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.Ads != null && context.Ads.IsInitialized;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.Ads.ShowInterstitialAd();
        Debug.Log("[DebugTools] Interstitial ad show requested.");
    }

    public bool GetToggleState(DebugContext context) => false;
}
