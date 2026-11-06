using UnityEngine;

/// <summary>Single tagged logger for the ads stack. Verbose lines are dropped unless enabled (Editor and development builds).</summary>
public sealed class AdsLogger
{
    private const string Tag = "[Ads] ";

    private readonly bool verbose;

    public AdsLogger(bool verbose)
    {
        this.verbose = verbose;
    }

    public void Verbose(string message)
    {
        if (verbose)
            Debug.Log(Tag + message);
    }

    public void Info(string message) => Debug.Log(Tag + message);

    public void Warn(string message) => Debug.LogWarning(Tag + message);

    public void Error(string message) => Debug.LogError(Tag + message);
}
