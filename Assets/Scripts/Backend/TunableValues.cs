using System;

/// <summary>
/// The remotely tunable values with their offline defaults. Out-of-range or wrongly typed remote values
/// fall back per key, so one bad dashboard entry never discards the rest.
/// </summary>
[Serializable]
public sealed class TunableValues : ITunables
{
    public const string ContinueCoinCostKey = "continue_coin_cost";
    public const string ContinueExtraMovesKey = "continue_extra_moves";
    public const string InterstitialsEnabledKey = "interstitials_enabled";
    public const string Level1MoveLimitKey = "level1_move_limit";

    public int continueCoinCost = 300;
    public int continueExtraMoves = 3;
    public bool interstitialsEnabled = true;
    public int level1MoveLimit = 25;

    public int ContinueCoinCost => continueCoinCost;
    public int ContinueExtraMoves => continueExtraMoves;
    public bool InterstitialsEnabled => interstitialsEnabled;
    public int Level1MoveLimit => level1MoveLimit;

    /// <summary>
    /// Reads every key through <paramref name="lookup"/>, which returns the raw value or null when the key is absent.
    /// Integers are accepted from any integral type; a value of another type or outside its range keeps <paramref name="fallback"/>.
    /// </summary>
    public static TunableValues Read(Func<string, object> lookup, TunableValues fallback)
    {
        return new TunableValues
        {
            continueCoinCost = ReadInt(lookup(ContinueCoinCostKey), fallback.continueCoinCost, 0, 100000),
            continueExtraMoves = ReadInt(lookup(ContinueExtraMovesKey), fallback.continueExtraMoves, 1, 50),
            interstitialsEnabled = lookup(InterstitialsEnabledKey) is bool enabled ? enabled : fallback.interstitialsEnabled,
            level1MoveLimit = ReadInt(lookup(Level1MoveLimitKey), fallback.level1MoveLimit, 1, 200)
        };
    }

    private static int ReadInt(object raw, int fallback, int min, int max)
    {
        long value;
        switch (raw)
        {
            case int i: value = i; break;
            case long l: value = l; break;
            case short s: value = s; break;
            case byte b: value = b; break;
            default: return fallback;
        }

        return value < min || value > max ? fallback : (int)value;
    }
}
