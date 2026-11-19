/// <summary>
/// Gameplay values the backend can change without a release. Values are stable within one level attempt;
/// a newer fetch applies from the next scene load.
/// </summary>
public interface ITunables
{
    int ContinueCoinCost { get; }
    int ContinueExtraMoves { get; }
    bool InterstitialsEnabled { get; }

    /// <summary>Move limit for the first level only; later levels keep their level data.</summary>
    int Level1MoveLimit { get; }
}
