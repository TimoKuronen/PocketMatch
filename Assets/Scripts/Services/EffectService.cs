using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

public class EffectService : IEffectService, IStartable, IDisposable
{
    private readonly Dictionary<string, ObjectPool<GameObject>> pools = new();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> handles = new();
    private Transform canvas;
    private const int DefaultSize = 10;

    [Inject]
    public void Construct()
    {
        canvas = GameObject.Find("VFXCanvas").transform;
    }

    public void Start()
    {
        PreloadEffectsByLabel(EffectKeys.TileVFXLabel);
    }

    public void PreloadEffectsByLabel(string label)
    {
        Debug.Log($"Preloading effects with label: {label}");
        TaskRunner.Instance.StartCoroutine(PreloadByLabelCoroutine(label));
    }

    private IEnumerator PreloadByLabelCoroutine(string label)
    {
        // First get all locations with this label
        var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return locationsHandle;

        if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
        {
            if (locationsHandle.IsValid())
                Addressables.Release(locationsHandle);
            yield break;
        }

        // Load each asset by its key
        foreach (var location in locationsHandle.Result)
        {
            var key = location.PrimaryKey;

            if (!pools.ContainsKey(key))
            {
                yield return LoadEffectCoroutine(key);
            }
        }

        if (locationsHandle.IsValid())
            Addressables.Release(locationsHandle);
    }

    private IEnumerator LoadEffectCoroutine(string key)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(key);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            handles[key] = handle;
            CreatePool(key, handle.Result);
        }
    }

    private void CreatePool(string key, GameObject prefab)
    {
        if (pools.ContainsKey(key)) return;

        var pool = new ObjectPool<GameObject>(
            createFunc: () => UnityEngine.Object.Instantiate(prefab, canvas),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: UnityEngine.Object.Destroy,
            collectionCheck: false,
            defaultCapacity: DefaultSize,
            maxSize: 50
        );

        pools[key] = pool;

        PrewarmVFX(pool);
    }

    private static void PrewarmVFX(ObjectPool<GameObject> pool)
    {
        List<GameObject> temp = new List<GameObject>();

        for (int i = 0; i < DefaultSize; i++)
            temp.Add(pool.Get());
        foreach (var obj in temp)
            pool.Release(obj);
    }

    public void PlayEffect(string effectKey, Vector3 position, Quaternion rotation = default)
    {
        if (!pools.TryGetValue(effectKey, out var pool))
        {
            Debug.LogWarning($"Pool for {effectKey} not found! Load it first.");
            return;
        }

        var instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);

        TaskRunner.Instance.StartCoroutine(ReturnToPoolCoroutine(instance, pool, GetEffectDuration(instance)));
    }

    private IEnumerator ReturnToPoolCoroutine(GameObject instance, ObjectPool<GameObject> pool, float duration)
    {
        yield return CachedCoroutines.Wait(duration);

        if (instance != null && instance.activeSelf)
            pool.Release(instance);
    }

    private float GetEffectDuration(GameObject effect)
    {
        var ps = effect.GetComponentInChildren<ParticleSystem>();
        return ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
    }

    public void Dispose()
    {
        foreach (var pool in pools.Values) 
            pool.Clear();

        foreach (var handle in handles.Values) 
            Addressables.Release(handle);

        pools.Clear();
        handles.Clear();
    }
}