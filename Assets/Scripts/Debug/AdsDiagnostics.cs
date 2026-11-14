using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Builds a PASS/FAIL report of the ads setup: local config shape and the live per-format state.
/// The Editor SDK is a stub, so network fill can only be judged from a device build.
/// </summary>
public static class AdsDiagnostics
{
    private const string ConfigFileName = "levelplay-config.json";
    private static readonly Regex FormatState = new Regex(@"(Banner|Interstitial|Rewarded)\(([^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex AdUnitIdShape = new Regex("^[a-z0-9]{10,24}$", RegexOptions.Compiled);

    public static string BuildReport(IAdsService ads)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Ads] ===== Ads diagnostics =====");
        sb.AppendLine($"Platform={Application.platform}, Bundle={Application.identifier}, Internet={Application.internetReachability}");

        if (Application.isEditor)
            sb.AppendLine("INFO  Editor SDK is a stub: init and load results here do not prove network fill.");

        AppendConfigChecks(sb);

        sb.AppendLine($"Simulation: {(AdsDebugConfig.IsSimulating ? "ON (results below are not from the network)" : "off (real network)")}");

        if (ads == null)
        {
            sb.AppendLine("FAIL  Ads service missing.");
        }
        else
        {
            string state = ads.Describe();
            sb.AppendLine($"State: {state}");
            AppendVerdict(sb, ads, state);
        }

        sb.Append("[Ads] ===== End =====");
        return sb.ToString();
    }

    // 509 means the mediation server answered with no advertiser (dashboard or demand side);
    // 626 means the ad unit id itself is rejected (config or dashboard mismatch).
    private static void AppendVerdict(StringBuilder sb, IAdsService ads, string state)
    {
        if (!ads.IsInitialized)
        {
            sb.AppendLine("VERDICT  SDK not initialized: check config, internet, and the app key against the dashboard.");
            return;
        }

        bool anyProblem = false;

        foreach (Match m in FormatState.Matches(state))
        {
            string format = m.Groups[1].Value;
            string body = m.Groups[2].Value;

            if (body.Contains(" 626,") || body.Contains(": 626"))
            {
                sb.AppendLine($"VERDICT  {format}: invalid ad unit id (626). Compare the id with the dashboard.");
                anyProblem = true;
            }
            else if (body.Contains(" 509,") || body.Contains(": 509"))
            {
                sb.AppendLine($"VERDICT  {format}: no fill (509). Your side is fine; network setup or demand.");
                anyProblem = true;
            }
            else if (!body.Contains("lastError=none"))
            {
                sb.AppendLine($"VERDICT  {format}: other load error. See lastError above.");
                anyProblem = true;
            }
        }

        if (!anyProblem)
            sb.AppendLine("VERDICT  No load errors recorded. Loads may still be pending.");
    }

    private static void AppendConfigChecks(StringBuilder sb)
    {
        if (!LocalJsonConfig.TryLoad(ConfigFileName, out LevelPlayConfig config))
        {
            sb.AppendLine($"FAIL  {ConfigFileName} missing or unreadable.");
            return;
        }

        sb.AppendLine($"PASS  {ConfigFileName} loaded.");
        Check(sb, "appKey", config.appKey, shapeCheck: false);
        Check(sb, "bannerAdId", config.bannerAdId, shapeCheck: true);
        Check(sb, "interstitialAdId", config.interstitialAdId, shapeCheck: true);
        Check(sb, "rewardedAdId", config.rewardedAdId, shapeCheck: true);

        var ids = new List<string>();
        AddIfSet(ids, config.bannerAdId);
        AddIfSet(ids, config.interstitialAdId);
        AddIfSet(ids, config.rewardedAdId);
        bool distinct = new HashSet<string>(ids).Count == ids.Count;
        sb.AppendLine(distinct ? "PASS  Ad unit ids are distinct." : "FAIL  Two formats share the same ad unit id.");
    }

    private static void Check(StringBuilder sb, string name, string value, bool shapeCheck)
    {
        string trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            sb.AppendLine($"FAIL  {name} is empty.");
            return;
        }

        if (trimmed.StartsWith("YOUR_"))
        {
            sb.AppendLine($"FAIL  {name} is still the example placeholder.");
            return;
        }

        if (value != trimmed)
            sb.AppendLine($"WARN  {name} has surrounding whitespace (trimmed at runtime).");

        if (shapeCheck && !AdUnitIdShape.IsMatch(trimmed))
        {
            sb.AppendLine($"WARN  {name} does not look like an ad unit id: {Mask(trimmed)}");
            return;
        }

        sb.AppendLine($"PASS  {name} set: {Mask(trimmed)}");
    }

    private static void AddIfSet(List<string> ids, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            ids.Add(value.Trim());
    }

    private static string Mask(string value) =>
        value.Length <= 4 ? "***" : $"{value.Substring(0, 3)}*** (len {value.Length})";
}
