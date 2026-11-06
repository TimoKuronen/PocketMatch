using System;

/// <summary>
/// Decides whether and when to retry a failed ad load. No-fill means no inventory right now, so it waits
/// a fixed long delay; other failures back off linearly; an invalid ad unit is never retried.
/// Counters are not cleared on exhaustion: call <see cref="Reset"/> on a successful load or a fresh request.
/// </summary>
public sealed class AdsRetryPolicy
{
    public enum FailureKind
    {
        Other,
        NoFill,
        InvalidAdUnit
    }

    private readonly int maxNoFillRetries;
    private readonly int maxOtherRetries;
    private readonly float noFillDelaySeconds;
    private readonly float backoffStepSeconds;

    private int noFillCount;
    private int otherCount;

    public AdsRetryPolicy(
        int maxNoFillRetries = 3,
        int maxOtherRetries = 3,
        float noFillDelaySeconds = 15f,
        float backoffStepSeconds = 3f)
    {
        this.maxNoFillRetries = maxNoFillRetries;
        this.maxOtherRetries = maxOtherRetries;
        this.noFillDelaySeconds = noFillDelaySeconds;
        this.backoffStepSeconds = backoffStepSeconds;
    }

    public static FailureKind Classify(string error)
    {
        if (string.IsNullOrEmpty(error))
            return FailureKind.Other;

        if (error.Contains("626") || error.IndexOf("invalid ad unit id", StringComparison.OrdinalIgnoreCase) >= 0)
            return FailureKind.InvalidAdUnit;

        if (error.Contains("509") || error.IndexOf("no fill", StringComparison.OrdinalIgnoreCase) >= 0)
            return FailureKind.NoFill;

        return FailureKind.Other;
    }

    /// <summary>Counts this failure and returns true with the delay when another attempt is allowed.</summary>
    public bool TryGetRetryDelay(FailureKind kind, out float delaySeconds)
    {
        delaySeconds = 0f;

        switch (kind)
        {
            case FailureKind.InvalidAdUnit:
                return false;

            case FailureKind.NoFill:
                noFillCount++;
                if (noFillCount > maxNoFillRetries)
                    return false;
                delaySeconds = noFillDelaySeconds;
                return true;

            default:
                otherCount++;
                if (otherCount > maxOtherRetries)
                    return false;
                delaySeconds = backoffStepSeconds * otherCount;
                return true;
        }
    }

    public void Reset()
    {
        noFillCount = 0;
        otherCount = 0;
    }
}
