using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Services : MonoBehaviour
{
    // --- Static singletons ---
    private static Services globalInstance;  
    private static Services currentSceneInstance;

    // --- Service storage ---
    private static readonly Dictionary<Type, IService> globalServices = new();
    private readonly Dictionary<Type, IService> sceneServices = new();
    private readonly List<IUpdateableService> updateableServices = new();

    public static bool AreGlobalServicesInitialized => globalServices.Count > 0;

    protected virtual void Awake()
    {
        if (this is GlobalServicesBootstrapper)
        {
            if (!AreGlobalServicesInitialized)
            {
                Debug.Log("Initializing global services in " + gameObject.name);
                globalInstance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGlobalServices();
            }
            else
            {
                Debug.Log("Global services already initialized, skipping in " + gameObject.name);
            }
        }
        else
        {
            Debug.Log("Setting current scene services: " + gameObject.name);
            currentSceneInstance = this;
            InitializeSceneServices();
        }
    }

    private void Update()
    {
        for (int i = 0; i < updateableServices.Count; i++)
            updateableServices[i].Update();
    }

    private void OnDestroy()
    {
        DisposeSceneServices();
    }

    protected virtual void InitializeGlobalServices() { }
    protected virtual void InitializeSceneServices() { }
    protected void InitializeAllSceneServices()
    {
        foreach (var service in sceneServices.Values)
        {
            service.Initialize();
        }
    }
    protected static void InitializeAllGlobalServices()
    {
        foreach (var service in globalServices.Values)
        {
            service.Initialize();
        }
    }

    protected void AddGlobalService<T>(T service) where T : IService
    {
        var key = typeof(T);
        if (globalServices.ContainsKey(key))
        {
            Debug.LogWarning($"Global service {key.Name} is already registered!");
            return;
        }

        globalServices[key] = service;
        if (service is IUpdateableService updateable)
            updateableServices.Add(updateable);
    }

    protected void AddSceneService<T>(T service) where T : IService
    {
        var key = typeof(T);
        if (sceneServices.ContainsKey(key))
        {
            Debug.LogWarning($"Scene service {key.Name} is already registered!");
            return;
        }

        sceneServices[key] = service;
        if (service is IUpdateableService updateable)
            updateableServices.Add(updateable);
    }

    private void DisposeSceneServices()
    {
        foreach (var service in sceneServices.Values)
            service.Dispose();

        sceneServices.Clear();
        updateableServices.Clear();
    }

    public static T Get<T>() where T : IService
    {
        var key = typeof(T);

        // 1. Global first
        if (globalServices.TryGetValue(key, out var globalService))
            return (T)globalService;

        // 2. Scene-specific
        if (currentSceneInstance != null &&
            currentSceneInstance.sceneServices.TryGetValue(key, out var sceneService))
            return (T)sceneService;

        Debug.LogError($"Service {key.Name} not found! " +
                       $"Globals: {string.Join(", ", globalServices.Keys.Select(k => k.Name))} | " +
                       $"Scene: {string.Join(", ", currentSceneInstance?.sceneServices.Keys.Select(k => k.Name) ?? Enumerable.Empty<string>())}");
        return default;
    }
}