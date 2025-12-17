using UnityEngine;

[CreateAssetMenu(fileName = "GridControllerSettings", menuName = "ScriptableObjects/GridControllerSettings", order = 1)]
public class GridControllerSettings : ScriptableObject
{
    [Header("Grid Settings")]
    public int width = 6;
    public int height = 8;
    public float tileSize = 1f;

    [Header("Prefab References")]
    public TileView normalTilePrefab;
    public TileView blockedTilePrefab;
    public TileView breakableTilePrefab;
    public RectTransform tileFramePrefab;

    [Header("Initial Debugging Settings")]
    public bool allowInitialMatches = false;
}





