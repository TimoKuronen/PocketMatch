using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;

public class EffectService : IEffectService, IStartable, IDisposable
{
    private readonly Dictionary<string, ObjectPool<GameObject>> pools = new();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> handles = new();
    private readonly Dictionary<string, Vector3> baseScales = new();
    private Transform canvas;
    private const int DefaultSize = 10;

    [Inject]
    public void Construct()
    {
        canvas = GameObject.Find("VFXCanvas").transform;
    }

    public void Start()
    {
        string label = EffectKeys.EffectsLabel;
        PreloadByLabelAsync(label).Forget();
    }

    private async UniTask PreloadByLabelAsync(string label)
    {
        var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        await locationsHandle.Task;

        if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
        {
            if (locationsHandle.IsValid())
                Addressables.Release(locationsHandle);
            return;
        }

        var tasks = new List<UniTask>();
        foreach (var location in locationsHandle.Result)
        {
            var key = location.PrimaryKey;

            if (!pools.ContainsKey(key))
                tasks.Add(LoadEffectAsync(location.PrimaryKey));
        }

        await UniTask.WhenAll(tasks);

        if (locationsHandle.IsValid())
            Addressables.Release(locationsHandle);
    }

    private async UniTask LoadEffectAsync(string key)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(key);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            handles[key] = handle;
            CreatePool(key, handle.Result);
        }
    }

    private void CreatePool(string key, GameObject prefab)
    {
        if (pools.ContainsKey(key)) return;

        Vector3 prefabScale = prefab.transform.localScale;
        baseScales[key] = prefabScale;

        var pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var instance = UnityEngine.Object.Instantiate(prefab, canvas);
                instance.transform.localScale = prefabScale;
                return instance;
            },
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

    public void PlayEffect(string effectKey, Vector3 position, Quaternion rotation = default, float scale = 1f)
    {
        if (!pools.TryGetValue(effectKey, out var pool))
        {
            Debug.LogWarning($"Pool for {effectKey} not found! Load it first.");
            return;
        }

        var instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);

        Vector3 baseScale = baseScales.TryGetValue(effectKey, out var stored)
            ? stored
            : Vector3.one;
        instance.transform.localScale = baseScale * scale;

        ReturnToPoolAsync(instance, pool, GetEffectDuration(instance)).Forget();
    }

    private async UniTaskVoid ReturnToPoolAsync(GameObject instance, ObjectPool<GameObject> pool, float duration)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration));

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
        baseScales.Clear();
    }
}
