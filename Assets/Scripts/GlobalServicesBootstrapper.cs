using System;
using UnityEngine;

public class GlobalServicesBootstrapper : Services
{
    private bool initialized = false;

    protected override void Initialize()
    {
        if (initialized)
        {
            Debug.Log("GlobalServicesBootstrapper is already initialized.");
            return;
        }
        else Debug.Log("GlobalServicesBootstrapper initializing...");
        
        initialized = true;

        var analyticsManager = new AnalyticsManager();
        AddService<IAnalyticsManager>(analyticsManager, isGlobal: true);

        foreach (var service in globalServices.Values)
        {
            Debug.Log($"[GlobalServicesBootstrapper] Initializing global service: {service.GetType().Name}");
            service.Initialize();
        }
    }
}
