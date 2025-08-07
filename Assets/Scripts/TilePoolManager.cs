using UnityEngine;
using UnityEngine.Pool;

public class TilePoolManager
{
    private readonly ObjectPool<TileView> normalPool;
    private readonly ObjectPool<TileView> blockedPool;
    private readonly ObjectPool<TileView> breakablePool;

    public TilePoolManager(TileView normalPrefab, TileView blockedPrefab, TileView breakablePrefab, Transform parent)
    {
        normalPool = new ObjectPool<TileView>(
            () => GameObject.Instantiate(normalPrefab, parent),
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false),
            t => GameObject.Destroy(t.gameObject),
            false, 100);

        blockedPool = new ObjectPool<TileView>(
            () => GameObject.Instantiate(blockedPrefab, parent),
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false),
            t => GameObject.Destroy(t.gameObject),
            false, 50);

        breakablePool = new ObjectPool<TileView>(
            () => GameObject.Instantiate(breakablePrefab, parent),
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false),
            t => GameObject.Destroy(t.gameObject),
            false, 50);
    }

    public TileView Get(TileState state)
    {
        return state switch
        {
            TileState.Normal => normalPool.Get(),
            TileState.Blocked => blockedPool.Get(),
            TileState.Destroyable => breakablePool.Get(),
            _ => throw new System.ArgumentOutOfRangeException(nameof(state))
        };
    }

    public void Release(TileView view, TileState state)
    {
        switch (state)
        {
            case TileState.Normal: normalPool.Release(view); break;
            case TileState.Blocked: blockedPool.Release(view); break;
            case TileState.Destroyable: breakablePool.Release(view); break;
            default: throw new System.ArgumentOutOfRangeException(nameof(state));
        }
    }
}