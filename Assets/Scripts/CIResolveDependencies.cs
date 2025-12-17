#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CIResolveDependencies
{
    [MenuItem("CI/Resolve Android Dependencies")]
    public static void Resolve()
    {
        Debug.Log("CI: Attempting to resolve Android dependencies via EDM (reflection)");

        // Load EDM editor assembly
        var asm = Assembly.Load("Google.VersionHandler");
        if (asm == null)
            throw new Exception("Google.VersionHandler assembly not found");

        // This type exists in all modern EDM versions
        var resolverType = asm.GetType("Google.VersionHandlerImpl");
        if (resolverType == null)
            throw new Exception("Google.VersionHandlerImpl type not found");

        // This method exists even though it's not public API
        var method = resolverType.GetMethod(
            "RunAndroidResolve",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
        );

        if (method == null)
            throw new Exception("RunAndroidResolve method not found");

        method.Invoke(null, null);

        Debug.Log("CI: Android dependency resolution completed");
    }
}
#endif
