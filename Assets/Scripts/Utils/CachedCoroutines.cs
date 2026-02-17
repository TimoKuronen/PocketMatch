using System.Collections.Generic;
using UnityEngine;

public static class CachedCoroutines
{
    private static readonly Dictionary<float, WaitForSeconds> waitCache = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds Wait(float duration)
    {
        if (!waitCache.TryGetValue(duration, out WaitForSeconds wait))
        {
            wait = new WaitForSeconds(duration);
            waitCache[duration] = wait;
        }
        return wait;
    }
}
