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
            "GooglePlayServices.PlayServicesResolver, Google.JarResolver");

        if (resolverType == null)
        {
            Debug.LogError("EDM Resolver not found");
            return;
        }

        var method = resolverType.GetMethod(
            "ResolveSync",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(bool) },
            null);

        if (method == null)
        {
            Debug.LogError("ResolveSync method not found");
            return;
        }

        method.Invoke(null, new object[] { true });

        Debug.Log("Android dependencies resolved successfully");
    }
}
