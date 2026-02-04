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
    // Dictionary to store pools for each effect type
    private Dictionary<string, ObjectPool<GameObject>> effectPools;
    
    // Dictionary to store loaded prefabs (from Addressables)
    private Dictionary<string, GameObject> loadedPrefabs;
    
    // Dictionary to store Addressables handles for proper cleanup
    private Dictionary<string, AsyncOperationHandle<GameObject>> addressableHandles;
    
    // Dictionary to track active effects
    private Dictionary<GameObject, string> activeEffects;
    
    // Parent transform for organizing effects in hierarchy
    private Transform effectsParent;
    
    // Preload settings
    private const int DEFAULT_POOL_SIZE = 10;
    private const int MAX_POOL_SIZE = 50;

    [Inject]
    public void Construct()
    {
        effectPools = new Dictionary<string, ObjectPool<GameObject>>();
        loadedPrefabs = new Dictionary<string, GameObject>();
        addressableHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        activeEffects = new Dictionary<GameObject, string>();
        
        // Create parent GameObject for effects
        var parentObj = new GameObject("EffectManager");
        effectsParent = parentObj.transform;
        UnityEngine.Object.DontDestroyOnLoad(parentObj);
    }

    public void PreloadEffects(string[] effectKeys)
    {
        foreach (var key in effectKeys)
        {
            if (!loadedPrefabs.ContainsKey(key))
            {
                CoroutineMonoBehavior.Instance.StartCoroutine(LoadAndPoolEffectCoroutine(key));
            }
        }
    }

    public void PreloadEffectsByLabel(string label)
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(PreloadEffectsByLabelCoroutine(label));
    }

    private IEnumerator PreloadEffectsByLabelCoroutine(string label)
    {
        var handle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            foreach (var location in handle.Result)
            {
                if (!loadedPrefabs.ContainsKey(location.PrimaryKey))
                {
                    yield return LoadAndPoolEffectCoroutine(location.PrimaryKey);
                }
            }
            Debug.Log($"[EffectService] Preloaded {handle.Result.Count} effects with label '{label}'");
        }
        else
        {
            Debug.LogError($"[EffectService] Failed to load locations for label '{label}'. Status: {handle.Status}");
        }

        Addressables.Release(handle);
    }

    private IEnumerator LoadAndPoolEffectCoroutine(string effectKey)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(effectKey);
        
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var prefab = handle.Result;
            loadedPrefabs[effectKey] = prefab;
            addressableHandles[effectKey] = handle;
            
            // Create pool for this effect
            CreatePoolForEffect(effectKey, prefab);
        }
        else
        {
            Debug.LogError($"Failed to load effect: {effectKey}. Status: {handle.Status}");
            Addressables.Release(handle);
        }
    }

    private void CreatePoolForEffect(string effectKey, GameObject prefab)
    {
        var pool = new ObjectPool<GameObject>(
            createFunc: () => UnityEngine.Object.Instantiate(prefab, effectsParent),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => UnityEngine.Object.Destroy(obj),
            collectionCheck: false,
            defaultCapacity: DEFAULT_POOL_SIZE,
            maxSize: MAX_POOL_SIZE
        );
        
        effectPools[effectKey] = pool;
    }

    public void PlayEffect(string effectKey, Vector3 position, Quaternion rotation = default)
    {
        if (string.IsNullOrEmpty(effectKey))
        {
            Debug.LogWarning("Effect key is null or empty");
            return;
        }

        if (!effectPools.ContainsKey(effectKey))
        {
            Debug.LogWarning($"Effect {effectKey} not preloaded. Attempting to load synchronously...");
            // Start loading and return early - effect will play once loaded
            CoroutineMonoBehavior.Instance.StartCoroutine(LoadAndPlayEffectWhenReady(effectKey, position, rotation));
            return;
        }

        var pool = effectPools[effectKey];
        var effectInstance = pool.Get();
        effectInstance.transform.position = position;
        effectInstance.transform.rotation = rotation;
        
        activeEffects[effectInstance] = effectKey;
        
        // Auto-return to pool when particle system finishes
        var particleSystem = effectInstance.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            float duration = main.duration + main.startLifetime.constantMax;
            CoroutineMonoBehavior.Instance.StartCoroutine(
                ReturnToPoolWhenFinished(effectInstance, effectKey, duration)
            );
        }
        else
        {
            // Fallback: return after fixed duration
            CoroutineMonoBehavior.Instance.StartCoroutine(
                ReturnToPoolWhenFinished(effectInstance, effectKey, 5f)
            );
        }
    }

    private IEnumerator LoadAndPlayEffectWhenReady(string effectKey, Vector3 position, Quaternion rotation)
    {
        yield return LoadAndPoolEffectCoroutine(effectKey);
        
        // Try to play again once loaded
        if (effectPools.ContainsKey(effectKey))
        {
            PlayEffect(effectKey, position, rotation);
        }
    }

    private IEnumerator ReturnToPoolWhenFinished(GameObject effectInstance, string effectKey, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReleaseEffect(effectInstance);
    }

    public void ReleaseEffect(GameObject effectInstance)
    {
        if (effectInstance == null)
            return;

        if (!activeEffects.TryGetValue(effectInstance, out var effectKey))
        {
            Debug.LogWarning("Trying to release an effect that is not tracked");
            return;
        }
            
        if (effectPools.TryGetValue(effectKey, out var pool))
        {
            pool.Release(effectInstance);
            activeEffects.Remove(effectInstance);
        }
        else
        {
            Debug.LogWarning($"Pool not found for effect key: {effectKey}");
        }
    }

    public void Dispose()
    {
        // Clean up all active effects
        foreach (var kvp in activeEffects)
        {
            if (kvp.Key != null)
            {
                ReleaseEffect(kvp.Key);
            }
        }
        activeEffects.Clear();

        // Release all Addressables handles
        foreach (var kvp in addressableHandles)
        {
            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
        }
        addressableHandles.Clear();
        loadedPrefabs.Clear();

        // Clear pools
        foreach (var pool in effectPools.Values)
        {
            pool.Dispose();
        }
        effectPools.Clear();

        // Destroy parent GameObject
        if (effectsParent != null)
        {
            UnityEngine.Object.Destroy(effectsParent.gameObject);
        }
    }
}
