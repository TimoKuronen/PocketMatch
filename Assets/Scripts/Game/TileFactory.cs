using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class TileFactory
{
    private readonly ObjectPool<TileView> pool;
    private readonly Sprite tileSprite;

    public TileFactory(ObjectPool<TileView> pool, Sprite sprite)
    {
        this.pool = pool;
        this.tileSprite = sprite;
    }

    public TileView CreateTile(TileType type, Vector2Int pos, TilePower power = TilePower.None)
    {
        var data = new TileData(type, pos, power);
        var view = pool.Get();

        view.transform.localScale = Vector3.one;
        view.Init(data, tileSprite);
        view.gameObject.name = $"{power}_{type}_{pos.x}_{pos.y}";

        return view;
    }
}
