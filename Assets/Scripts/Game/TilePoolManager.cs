using UnityEngine;
using UnityEngine.Pool;

public class TilePoolManager
{
    private readonly ObjectPool<TileView> normalPool;
    private readonly ObjectPool<TileView> blockedPool;
    private readonly ObjectPool<TileView> breakablePool;
    private readonly IGridController gridController;

    public TilePoolManager(TileView normalPrefab, TileView blockedPrefab, TileView breakablePrefab, Transform parent, IGridController gridController)
    {
        this.gridController = gridController;

        normalPool = new ObjectPool<TileView>(
            () =>
            {
                var view = GameObject.Instantiate(normalPrefab, parent);
                InjectGridController(view.gameObject);
                return view;
            },
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false),
            t =>
            {
                if (t != null && t.gameObject != null)
                    GameObject.Destroy(t.gameObject);
            },
            false, 100);

        blockedPool = new ObjectPool<TileView>(
            () =>
            {
                var view = GameObject.Instantiate(blockedPrefab, parent);
                InjectGridController(view.gameObject);
                return view;
            },
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false),
            t =>
            {
                if (t != null && t.gameObject != null)
                    GameObject.Destroy(t.gameObject);
            },
            false, 50);

        breakablePool = new ObjectPool<TileView>(
            () =>
            {
                var view = GameObject.Instantiate(breakablePrefab, parent);
                InjectGridController(view.gameObject);
                return view;
            },
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false),
            t =>
            {
                if (t != null && t.gameObject != null)
                    GameObject.Destroy(t.gameObject);
            },
            false, 50);
    }

    private void InjectGridController(GameObject tileObject)
    {
        var inputHandler = tileObject.GetComponent<TileInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.Construct(gridController);
        }
    }

    public TileView GetForState(TileState state)
    {
        var effective = state == TileState.Empty ? TileState.Normal : state;
        TileView view = effective switch
        {
            TileState.Normal => normalPool.Get(),
            TileState.Blocked => blockedPool.Get(),
            TileState.Destroyable => breakablePool.Get(),
            _ => normalPool.Get()
        };

        view.ViewKind = effective;
        return view;
    }

    public void Release(TileView view)
    {
        switch (view.ViewKind)
        {
            case TileState.Normal: normalPool.Release(view); break;
            case TileState.Blocked: blockedPool.Release(view); break;
            case TileState.Destroyable: breakablePool.Release(view); break;
            default: normalPool.Release(view); break;
        }
    }
}