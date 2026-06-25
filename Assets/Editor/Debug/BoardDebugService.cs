using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor-only board debug logging. Subscribes to BoardDebugHooks at editor load.
/// </summary>
public static class BoardDebugService
{
    public static void OnBoardInitialized(GridController controller, TileData[,] data)
    {
        if (!BoardDebugConfig.IsEnabled)
            return;

        if (controller == null || data == null)
            return;

        int w = controller.GridWidthForValidation;
        int h = controller.GridHeightForValidation;
        BoardDebugLogger.Instance.LogBoard("BoardInitialized", data, w, h);
        RunInvariants(controller, data, "BoardInitialized");
    }

    public static void OnBoardUpdated(GridController controller, TileData[,] data)
    {
        if (!BoardDebugConfig.IsEnabled)
            return;

        if (controller == null || data == null)
            return;

        int w = controller.GridWidthForValidation;
        int h = controller.GridHeightForValidation;

        BoardDebugLogger.Instance.LogBoard("BoardUpdated", data, w, h);
        RunInvariants(controller, data, "BoardUpdated");
    }

    public static void OnBoardShuffled(GridController controller, TileData[,] data)
    {
        if (!BoardDebugConfig.IsEnabled)
            return;

        if (controller == null || data == null)
            return;

        int w = controller.GridWidthForValidation;
        int h = controller.GridHeightForValidation;

        BoardDebugLogger.Instance.LogBoard("BoardShuffled", data, w, h);
        RunInvariants(controller, data, "BoardShuffled");
    }

    private static void RunInvariants(GridController controller, TileData[,] data, string sourceEvent)
    {
        int w = controller.GridWidthForValidation;
        int h = controller.GridHeightForValidation;

        if (!BoardInvariants.CheckNoColumnHoles(data, w, h, out var msg, out List<Vector2Int> offenders))
        {
            var extra = new Dictionary<string, string>
            {
                { "reason", msg },
                { "offenders", string.Join(";", offenders) },
                { "source", sourceEvent }
            };
            BoardDebugLogger.Instance.LogBoard("Anomaly_ColumnHoles", data, w, h, extra);
            Debug.LogWarning($"[BoardDebugService] {msg} (from {sourceEvent})");
        }
    }
}
