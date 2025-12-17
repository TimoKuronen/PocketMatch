using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

public static class CIResolveDependencies
{
    [MenuItem("CI/Resolve Android Dependencies")]
    public static void Resolve()
    {
        var resolverType = Type.GetType(
            "Google.PlayServices.Resolver, Google.VersionHandlerImpl");

        if (resolverType == null)
        {
            Debug.LogError("EDM Resolver not found");
            return;
        }

        var method = resolverType.GetMethod(
            "ResolveSync",
            BindingFlags.Static | BindingFlags.Public);

        if (method == null)
        {
            Debug.LogError("ResolveSync method not found");
            return;
        }

        method.Invoke(null, new object[] { true });

        Debug.Log("Android dependencies resolved successfully");
    }
}
