using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

public class EffectService : IEffectService, IDisposable
{
    private Dictionary<string, ObjectPool<GameObject>> effectPools;
    private Dictionary<string, GameObject> loadedPrefabs;
    private Dictionary<string, AsyncOperationHandle<GameObject>> addressableHandles;
    private Dictionary<GameObject, string> activeEffects;
    private HashSet<string> failedEffects;

    private const int DEFAULT_POOL_SIZE = 10;
    private const int MAX_POOL_SIZE = 50;
    Transform vfxCanvasTransform;

    [Inject]
    public void Construct()
    {
        effectPools = new Dictionary<string, ObjectPool<GameObject>>();
        loadedPrefabs = new Dictionary<string, GameObject>();
        addressableHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        activeEffects = new Dictionary<GameObject, string>();
        failedEffects = new HashSet<string>();

        if (vfxCanvasTransform == null)
        {
            var canvasGo = GameObject.Find("VFXCanvas");
            if (canvasGo != null) vfxCanvasTransform = canvasGo.transform;
        }
    }

    public void PreloadEffects(string[] effectKeys)
    {
        foreach (var key in effectKeys)
        {
            if (!loadedPrefabs.ContainsKey(key) && !failedEffects.Contains(key))
            {
                CoroutineMonoBehavior.Instance.StartCoroutine(LoadEffectCoroutine(key));
            }
        }
    }

    public void PreloadEffectsByLabel(string label)
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(PreloadByLabelCoroutine(label));
    }

    private IEnumerator PreloadByLabelCoroutine(string label)
    {
        var handle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogWarning($"[EffectService] Label '{label}' not found or failed to load.");
            if (handle.IsValid()) Addressables.Release(handle);
            yield break;
        }

        int loadedCount = 0;
        foreach (var location in handle.Result)
        {
            if (!loadedPrefabs.ContainsKey(location.PrimaryKey) && !failedEffects.Contains(location.PrimaryKey))
            {
                yield return LoadEffectCoroutine(location.PrimaryKey);
                if (effectPools.ContainsKey(location.PrimaryKey)) loadedCount++;
            }
        }

        Debug.Log($"[EffectService] Preloaded {loadedCount}/{handle.Result.Count} effects with label '{label}'");
        if (handle.IsValid()) Addressables.Release(handle);
    }

    private IEnumerator LoadEffectCoroutine(string effectKey)
    {
        // Check if location exists first (prevents InvalidKeyException)
        var locationHandle = Addressables.LoadResourceLocationsAsync(effectKey, typeof(GameObject));
        yield return locationHandle;

        if (locationHandle.Status != AsyncOperationStatus.Succeeded || 
            locationHandle.Result == null || 
            locationHandle.Result.Count == 0)
        {
            MarkAsFailed(effectKey, "not found in Addressables");
            if (locationHandle.IsValid()) 
                Addressables.Release(locationHandle);

            yield break;
        }

        if (locationHandle.IsValid()) 
            Addressables.Release(locationHandle);

        // Load the asset
        var handle = Addressables.LoadAssetAsync<GameObject>(effectKey);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || !handle.IsValid())
        {
            MarkAsFailed(effectKey, $"load failed: {handle.Status}");
            if (handle.IsValid()) Addressables.Release(handle);
            yield break;
        }

        // Success - create pool
        var prefab = handle.Result;
        loadedPrefabs[effectKey] = prefab;
        addressableHandles[effectKey] = handle;
        CreatePoolForEffect(effectKey, prefab);
        failedEffects.Remove(effectKey);
    }

    private void MarkAsFailed(string effectKey, string reason)
    {
        failedEffects.Add(effectKey);
        Debug.LogWarning($"[EffectService] Effect '{effectKey}' {reason}. Will be skipped.");
    }

    private void CreatePoolForEffect(string effectKey, GameObject prefab)
    {
        var pool = new ObjectPool<GameObject>(
            createFunc: () => {
                var obj = UnityEngine.Object.Instantiate(prefab);
                obj.SetActive(false);
                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => UnityEngine.Object.Destroy(obj),
            collectionCheck: false,
            defaultCapacity: DEFAULT_POOL_SIZE,
            maxSize: MAX_POOL_SIZE
        );

        Stack<GameObject> tempPrewarmList = new Stack<GameObject>();
        for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
        {
            tempPrewarmList.Push(pool.Get());
        }
        while (tempPrewarmList.Count > 0)
        {
            pool.Release(tempPrewarmList.Pop());
        }

        effectPools[effectKey] = pool;
    }

    public void PlayEffect(string effectKey, Vector3 worldPosition, Quaternion rotation = default)
    {
        Debug.Log($"[VFX] PlayEffect called for: {effectKey} at {worldPosition}");

        if (effectPools.TryGetValue(effectKey, out var pool))
        {
            SpawnEffect(pool, effectKey, worldPosition, rotation);
        }
        else
        {
            Debug.LogWarning($"[VFX] Pool for {effectKey} not found! Is it loaded?");
            // If not loaded, we load it. 
            CoroutineMonoBehavior.Instance.StartCoroutine(LoadAndPlayCoroutine(effectKey, worldPosition, rotation));
        }
    }

    private void SpawnEffect(ObjectPool<GameObject> pool, string effectKey, Vector3 worldPosition, Quaternion rotation)
    {
        var instance = pool.Get();
        instance.SetActive(true);

        instance.transform.SetParent(vfxCanvasTransform, false);
        instance.transform.position = worldPosition;

        instance.transform.localPosition = new Vector3(
            instance.transform.localPosition.x,
            instance.transform.localPosition.y,
            0f
        );

        activeEffects[instance] = effectKey;

        //if (instance.TryGetComponent<ParticleSystem>(out var ps))
        //{
        //    ps.Play();
        CoroutineMonoBehavior.Instance.StartCoroutine(ReturnToPoolWhenFinished(instance, effectKey, 2f));
        //}
    }

    private IEnumerator LoadAndPlayCoroutine(string effectKey, Vector3 position, Quaternion rotation)
    {
        if (failedEffects.Contains(effectKey)) yield break;

        yield return LoadEffectCoroutine(effectKey);

        if (effectPools.TryGetValue(effectKey, out var pool))
        {
            SpawnEffect(pool, effectKey, position, rotation);
        }
    }

    private IEnumerator ReturnToPoolWhenFinished(GameObject instance, string effectKey, float duration)
    {
        yield return new WaitForSeconds(duration);

        ReleaseEffect(instance);
    }

    public void ReleaseEffect(GameObject effectInstance)
    {
        if (effectInstance == null || !activeEffects.TryGetValue(effectInstance, out var effectKey))
            return;

        if (effectPools.TryGetValue(effectKey, out var pool))
        {
            pool.Release(effectInstance);
            activeEffects.Remove(effectInstance);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in activeEffects)
        {
            if (kvp.Key != null) ReleaseEffect(kvp.Key);
        }
        activeEffects.Clear();

        foreach (var kvp in addressableHandles)
        {
            if (kvp.Value.IsValid()) 
                Addressables.Release(kvp.Value);
        }

        addressableHandles.Clear();
        loadedPrefabs.Clear();

        foreach (var pool in effectPools.Values)
        {
            pool.Dispose();
        }
        effectPools.Clear();
    }
}
