using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Increments Android bundle version code before local player builds.
/// CI supplies androidVersionCode separately, so this skips when CI=true.
/// </summary>
public class BuildVersionIncrementer : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        string ci = Environment.GetEnvironmentVariable("CI");
        if (string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[BuildVersionIncrementer] Skipping increment (CI=true); CI supplies androidVersionCode.");
            return;
        }

        int previous = PlayerSettings.Android.bundleVersionCode;
        int next = previous + 1;
        PlayerSettings.Android.bundleVersionCode = next;
        Debug.Log($"[BuildVersionIncrementer] AndroidBundleVersionCode {previous} -> {next}");
    }
}
