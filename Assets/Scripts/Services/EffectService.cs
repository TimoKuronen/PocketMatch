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
    private Dictionary<string, ObjectPool<GameObject>> effectPools;
    private Dictionary<string, GameObject> loadedPrefabs;
    private Dictionary<string, AsyncOperationHandle<GameObject>> addressableHandles;
    private Dictionary<GameObject, string> activeEffects;
    private HashSet<string> failedEffects;
    private Dictionary<string, Transform> poolParents; // Store pool parent transforms for cleanup

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
        poolParents = new Dictionary<string, Transform>();

        if (vfxCanvasTransform == null)
        {
            var canvasGo = GameObject.Find("VFXCanvas");
            if (canvasGo != null) vfxCanvasTransform = canvasGo.transform;
        }
    }

    public void Start()
    {
        // Ensure VFXCanvas is found
        if (vfxCanvasTransform == null)
        {
            var canvasGo = GameObject.Find("VFXCanvas");
            if (canvasGo != null) vfxCanvasTransform = canvasGo.transform;
        }

        // Preload all effects with the TileVFX label
        PreloadEffectsByLabel(EffectKeys.TileVFXLabel);
        Debug.Log("[EffectService] Started preloading effects with label: " + EffectKeys.TileVFXLabel);
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
        if (prefab == null)
        {
            MarkAsFailed(effectKey, "prefab result is null");
            if (handle.IsValid()) Addressables.Release(handle);
            yield break;
        }

        Debug.Log($"[EffectService] Successfully loaded prefab for {effectKey}: {prefab.name}");
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
        // Validate prefab
        if (prefab == null)
        {
            Debug.LogError($"[EffectService] Cannot create pool for {effectKey}: prefab is null!");
            MarkAsFailed(effectKey, "prefab is null");
            return;
        }

        Debug.Log($"[EffectService] Creating pool for {effectKey} with prefab: {prefab.name}");

        // Ensure VFXCanvas is available
        if (vfxCanvasTransform == null)
        {
            var canvasGo = GameObject.Find("VFXCanvas");
            if (canvasGo != null) vfxCanvasTransform = canvasGo.transform;
        }

        // Create a hidden parent for inactive pooled objects to prevent Unity from destroying them
        Transform poolParent = null;
        if (vfxCanvasTransform != null)
        {
            var poolParentGo = new GameObject($"{effectKey}_Pool");
            poolParentGo.SetActive(false); // Hide the parent
            poolParent = poolParentGo.transform;
            poolParent.SetParent(vfxCanvasTransform, false);
            poolParents[effectKey] = poolParent; // Store for cleanup
        }

        // Capture poolParent and prefab in closure
        var capturedPoolParent = poolParent;
        var capturedVfxCanvas = vfxCanvasTransform;
        var capturedPrefab = prefab; // Capture prefab reference

        var pool = new ObjectPool<GameObject>(
            createFunc: () => {
                if (capturedPrefab == null)
                {
                    Debug.LogError($"[EffectService] Prefab for {effectKey} is null in createFunc!");
                    return null;
                }

                try
                {
                    var obj = UnityEngine.Object.Instantiate(capturedPrefab);
                    if (obj == null)
                    {
                        Debug.LogError($"[EffectService] Instantiate returned null for {effectKey}");
                        return null;
                    }
                    
                    obj.SetActive(false);
                    // Parent to pool parent (or VFXCanvas if pool parent doesn't exist) to prevent destruction
                    if (capturedPoolParent != null)
                    {
                        obj.transform.SetParent(capturedPoolParent, false);
                    }
                    else if (capturedVfxCanvas != null)
                    {
                        obj.transform.SetParent(capturedVfxCanvas, false);
                    }
                    
                    Debug.Log($"[EffectService] Created pooled instance for {effectKey}");
                    return obj;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EffectService] Exception creating instance for {effectKey}: {ex}");
                    return null;
                }
            },
            actionOnGet: (obj) => {
                // Unity's == operator handles destroyed objects
                if (obj != null)
                {
                    try
                    {
                        obj.SetActive(true);
                    }
                    catch (MissingReferenceException)
                    {
                        // Object was destroyed, pool will create a new one
                        Debug.LogWarning($"[EffectService] Object in pool was destroyed, will create new instance");
                    }
                }
            },
            actionOnRelease: (obj) => {
                // Unity's == operator handles destroyed objects
                if (obj != null)
                {
                    try
                    {
                        obj.SetActive(false);
                        // Return to pool parent when released
                        if (capturedPoolParent != null)
                        {
                            obj.transform.SetParent(capturedPoolParent, false);
                        }
                    }
                    catch (MissingReferenceException)
                    {
                        // Object was destroyed, ignore
                    }
                }
            },
            actionOnDestroy: (obj) => {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            },
            collectionCheck: false,
            defaultCapacity: DEFAULT_POOL_SIZE,
            maxSize: MAX_POOL_SIZE
        );

        // Prewarm the pool
        Stack<GameObject> tempPrewarmList = new Stack<GameObject>();
        int successfulPrewarms = 0;
        for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
        {
            var obj = pool.Get();
            if (obj != null)
            {
                tempPrewarmList.Push(obj);
                successfulPrewarms++;
            }
            else
            {
                Debug.LogWarning($"[EffectService] Failed to prewarm pool instance {i} for {effectKey}");
            }
        }
        
        Debug.Log($"[EffectService] Prewarmed {successfulPrewarms}/{DEFAULT_POOL_SIZE} instances for {effectKey}");
        
        while (tempPrewarmList.Count > 0)
        {
            var obj = tempPrewarmList.Pop();
            if (obj != null)
            {
                pool.Release(obj);
            }
        }

        if (successfulPrewarms == 0)
        {
            Debug.LogError($"[EffectService] Failed to create any pool instances for {effectKey}! Pool may not work correctly.");
            MarkAsFailed(effectKey, "failed to create pool instances");
            pool.Dispose();
            return;
        }

        effectPools[effectKey] = pool;
        Debug.Log($"[EffectService] Successfully created pool for {effectKey}");
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
        if (instance == null)
        {
            Debug.LogError($"[EffectService] Failed to get instance from pool for {effectKey}. Attempting to recreate pool...");
            
            // Try to recreate the pool if prefab is still available
            if (loadedPrefabs.TryGetValue(effectKey, out var prefab) && prefab != null)
            {
                Debug.Log($"[EffectService] Recreating pool for {effectKey}");
                // Dispose old pool if it exists
                if (effectPools.TryGetValue(effectKey, out var oldPool))
                {
                    oldPool.Dispose();
                }
                CreatePoolForEffect(effectKey, prefab);
                
                // Try again
                if (effectPools.TryGetValue(effectKey, out var newPool))
                {
                    instance = newPool.Get();
                    if (instance == null)
                    {
                        Debug.LogError($"[EffectService] Still failed to get instance after recreating pool for {effectKey}");
                        return;
                    }
                }
                else
                {
                    Debug.LogError($"[EffectService] Failed to recreate pool for {effectKey}");
                    return;
                }
            }
            else
            {
                Debug.LogError($"[EffectService] Cannot recreate pool - prefab not available for {effectKey}");
                return;
            }
        }

        // Ensure VFXCanvas is available
        if (vfxCanvasTransform == null)
        {
            var canvasGo = GameObject.Find("VFXCanvas");
            if (canvasGo != null) vfxCanvasTransform = canvasGo.transform;
        }

        // Move to VFXCanvas and set position
        if (vfxCanvasTransform != null)
        {
            instance.transform.SetParent(vfxCanvasTransform, false);
        }
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
        else
        {
            // Pool doesn't exist, just destroy the instance
            if (effectInstance != null)
            {
                UnityEngine.Object.Destroy(effectInstance);
            }
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

        // Clean up pool parent GameObjects
        foreach (var poolParent in poolParents.Values)
        {
            if (poolParent != null && poolParent.gameObject != null)
            {
                UnityEngine.Object.Destroy(poolParent.gameObject);
            }
        }
        poolParents.Clear();
    }
}
