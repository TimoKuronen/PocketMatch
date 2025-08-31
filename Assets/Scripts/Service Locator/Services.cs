using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Services : MonoBehaviour
{
    private static Services instance;

    protected Dictionary<Type, IService> serviceMap = new Dictionary<Type, IService>();
    // TODO : Should we make this non-static and have a separate persistent Services object in each scene?
    protected static Dictionary<Type, IService> globalServices = new Dictionary<Type, IService>(); 
    private List<IUpdateableService> updateableServices = new List<IUpdateableService>();

    private void Awake()
    {
        instance = this;

        Initialize();

        var monobehaviorServices = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IService>();

        foreach (var service in monobehaviorServices)
        {
            if (serviceMap.ContainsKey(service.GetType()))
            {
                continue;
            }

            AddService(service);
        }
    }

    /// <summary>
    /// Initialize is called before all other awake methods in the game.
    /// This is where you should set up all services.
    /// </summary>
    protected abstract void Initialize();

    /// <summary>
    /// Registers a service, making it available through the Get method.
    /// </summary>
    public void AddService<T>(T service, bool isGlobal = false) where T : IService
    {
        var key = typeof(T);

        if (isGlobal)
        {
            if (globalServices.ContainsKey(key))
                Debug.LogWarning($"{key} already registered as global.");
            else
                globalServices[key] = service;
        }
        else
        {
            if (serviceMap.ContainsKey(key))
                Debug.LogWarning($"{key} already registered in this scene.");
            else
                serviceMap[key] = service;
        }

        //Debug.Log($"Adding service from MonoBehaviour: {service.GetType().Name}");

        if (service is IUpdateableService updateableService)
            updateableServices.Add(updateableService);
    }

    public static T Get<T>() where T : IService
    {
        var key = typeof(T);

        if (instance.serviceMap.TryGetValue(key, out IService sceneService))
            return (T)sceneService;

        if (globalServices.TryGetValue(key, out IService globalService))
            return (T)globalService;

        Debug.LogError($"Service not found: {key.Name}");
        foreach (var service in instance.serviceMap.Values)
        {
            Debug.Log($"Registered scene service: {service.GetType().Name}");
        }
        foreach (var service in globalServices.Values)
        {
            Debug.Log($"Registered global service: {service.GetType().Name}");
        }
        return default;
    }

    private void Update()
    {
        for (int i = 0; i < updateableServices.Count; i++)
        {
            updateableServices[i].Update();
        }
    }

    private void OnDisable()
    {
        CallToDispose();
    }

    private void OnDestroy()
    {
        CallToDispose();
    }

    private void CallToDispose()
    {
        //Debug.Log("Disposing Services...");

        // Dispose services in serviceMap
        foreach (KeyValuePair<Type, IService> service in serviceMap)
        {
            service.Value.Dispose();
        }

        // Dispose services in updateableServices
        foreach (var updateableService in updateableServices)
        {
            updateableService.Dispose();
        }
    }
}
