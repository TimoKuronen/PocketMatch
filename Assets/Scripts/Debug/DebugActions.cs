using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;
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
    public const string HideBannerAd = "hide_banner_ad";
    public const string ShowRewardedAd = "show_rewarded_ad";
    public const string LaunchAdsTestSuite = "launch_ads_test_suite";
    public const string ToggleSkipInterstitials = "toggle_skip_interstitials";
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

public sealed class HideBannerAdDebugAction : IDebugAction
{
    public string Id => DebugActionIds.HideBannerAd;
    public string Category => "Ads";
    public string Label => "Hide Banner Ad";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.Ads != null && context.Ads.IsInitialized;

    public void Execute(DebugContext context, int intValue = 0)
    {
        context.Ads.HideBannerAd();
    }

    public bool GetToggleState(DebugContext context) => false;
}

public sealed class ShowRewardedAdDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ShowRewardedAd;
    public string Category => "Ads";
    public string Label => "Show Rewarded Ad";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.Ads != null && context.Ads.IsInitialized;

    public void Execute(DebugContext context, int intValue = 0)
    {
        ShowAsync(context.Ads).Forget();
    }

    public bool GetToggleState(DebugContext context) => false;

    private static async UniTaskVoid ShowAsync(IAdsService ads)
    {
        var result = await ads.ShowRewardedAdAsync();
        Debug.Log($"[DebugTools] Rewarded ad result: {result}");
    }
}

public sealed class ToggleSkipInterstitialsDebugAction : IDebugAction
{
    public string Id => DebugActionIds.ToggleSkipInterstitials;
    public string Category => "Ads";
    public string Label => "Skip Interstitial Gate";
    public DebugActionKind Kind => DebugActionKind.Toggle;

    public bool IsAvailable(DebugContext context) => true;

    public void Execute(DebugContext context, int intValue = 0)
    {
        AdsDebugConfig.SkipInterstitials = intValue != 0;
    }

    public bool GetToggleState(DebugContext context) => AdsDebugConfig.SkipInterstitials;
}

public sealed class LaunchAdsTestSuiteDebugAction : IDebugAction
{
    public string Id => DebugActionIds.LaunchAdsTestSuite;
    public string Category => "Ads";
    public string Label => "LevelPlay Test Suite";
    public DebugActionKind Kind => DebugActionKind.Button;

    public bool IsAvailable(DebugContext context) => context.Ads != null && context.Ads.IsInitialized;

    public void Execute(DebugContext context, int intValue = 0)
    {
        LevelPlay.LaunchTestSuite();
    }

    public bool GetToggleState(DebugContext context) => false;
}
