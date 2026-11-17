using System;
using System.Globalization;

/// <summary>
/// Decides which copy of the save survives when local and cloud both exist. Progress only moves forward,
/// so the copy with more levels unlocked wins; equal progress goes to the more recent save. No field merge.
/// </summary>
public static class SaveConflictResolver
{
    public static bool CloudWins(PlayerData local, PlayerData cloud)
    {
        if (cloud == null)
            return false;

        if (local == null)
            return true;

        if (cloud.nextLevelIndex != local.nextLevelIndex)
            return cloud.nextLevelIndex > local.nextLevelIndex;

        return IsNewer(cloud, local);
    }

    private static bool IsNewer(PlayerData candidate, PlayerData other)
    {
        bool candidateValid = TryParseTime(candidate, out var candidateTime);
        bool otherValid = TryParseTime(other, out var otherTime);

        if (!candidateValid)
            return false;

        return !otherValid || candidateTime > otherTime;
    }

    private static bool TryParseTime(PlayerData data, out DateTime time)
    {
        time = default;
        string text = data.meta?.lastSaveTime;
        return !string.IsNullOrEmpty(text) &&
               DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out time);
    }
}
