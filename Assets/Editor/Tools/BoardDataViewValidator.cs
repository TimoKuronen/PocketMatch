#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to validate that GridController's gridData and gridViews stay in sync.
/// </summary>
public static class BoardDataViewValidator
{
    private const string MenuPath = "Limekicker/Validate Board Data vs View";

    [MenuItem(MenuPath)]
    public static void ValidateBoardDataVsView()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[BoardDataViewValidator] Game must be in Play mode.");
            return;
        }

        if (!Loader.IsGameScene())
        {
            Debug.LogWarning("[BoardDataViewValidator] Active scene must be the game scene.");
            return;
        }

        var gridController = Object.FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            Debug.LogError("[BoardDataViewValidator] No GridController found in scene.");
            return;
        }

        var data = gridController.GridDataForValidation;
        var views = gridController.GridViewsForValidation;
        int w = gridController.GridWidthForValidation;
        int h = gridController.GridHeightForValidation;

        if (data == null || views == null)
        {
            Debug.LogError("[BoardDataViewValidator] Grid data or views are null (board may not be initialized).");
            return;
        }

        int dataW = data.GetLength(0);
        int dataH = data.GetLength(1);
        int viewW = views.GetLength(0);
        int viewH = views.GetLength(1);

        if (dataW != w || dataH != h || viewW != w || viewH != h)
        {
            Debug.LogError($"[BoardDataViewValidator] Dimension mismatch: data={dataW}x{dataH}, views={viewW}x{viewH}, reported size={w}x{h}");
            return;
        }

        var mismatches = new List<string>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var dataCell = data[x, y];
                var viewCell = views[x, y];
                bool dataIsEmptyOrNull = dataCell == null || dataCell.State == TileState.Empty;

                if (dataIsEmptyOrNull)
                {
                    if (viewCell != null && viewCell.gameObject.activeInHierarchy)
                    {
                        mismatches.Add($"({x},{y}): Data is null/Empty but view exists and is active.");
                    }
                    continue;
                }

                if (dataCell.State == TileState.Blocked)
                {
                    if (viewCell == null || !viewCell.gameObject.activeInHierarchy)
                    {
                        mismatches.Add($"({x},{y}): Data is Blocked but view is null or inactive.");
                    }
                    else if (!TileDataMatchesView(dataCell, viewCell))
                    {
                        mismatches.Add($"({x},{y}): Blocked tile view.Data does not match gridData.");
                    }
                    continue;
                }

                if (viewCell == null || !viewCell.gameObject.activeInHierarchy)
                {
                    mismatches.Add($"({x},{y}): Data has {dataCell.State} but view is null or inactive.");
                    continue;
                }

                if (viewCell.Data == null)
                {
                    mismatches.Add($"({x},{y}): View exists but view.Data is null.");
                    continue;
                }

                if (!TileDataMatchesView(dataCell, viewCell))
                {
                    mismatches.Add($"({x},{y}): view.Data does not match gridData (Type/Power/Position/State).");
                }
            }
        }

        if (mismatches.Count == 0)
        {
            Debug.Log($"[BoardDataViewValidator] OK: All {w * h} cells consistent (data vs view).");
            return;
        }

        foreach (var msg in mismatches)
        {
            Debug.LogError($"[BoardDataViewValidator] {msg}");
        }

        Debug.LogError($"[BoardDataViewValidator] Found {mismatches.Count} mismatch(es). See above.");
    }

    private static bool TileDataMatchesView(TileData data, TileView view)
    {
        if (view.Data == null) return false;
        var v = view.Data;
        return data.Type == v.Type
               && data.Power == v.Power
               && data.GridPosition == v.GridPosition
               && data.State == v.State;
    }
}
#endif
