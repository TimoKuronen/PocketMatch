using System;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

/// <summary>
/// Editor and development builds use the development environment so test data stays apart from release data.
/// </summary>
public sealed class UgsInitializer : IUgsInitializer
{
    private const string DevelopmentEnvironment = "development";
    private const string ProductionEnvironment = "production";

    private UniTask<bool> pending;
    private bool started;
    private UniTask<bool> pendingSignIn;
    private bool signInStarted;

    public UniTask<bool> InitializeAsync()
    {
        if (!started)
        {
            started = true;
            pending = RunAsync().Preserve();
        }

        return pending;
    }

    public UniTask<bool> EnsureSignedInAsync()
    {
        if (!signInStarted)
        {
            signInStarted = true;
            pendingSignIn = SignInAsync().Preserve();
        }

        return pendingSignIn;
    }

    private async UniTask<bool> SignInAsync()
    {
        try
        {
            if (!await InitializeAsync())
            {
                signInStarted = false;
                return false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return true;
        }
        catch (Exception e)
        {
            signInStarted = false;
            Debug.LogWarning($"[UGS] Anonymous sign-in failed: {e.Message}");
            return false;
        }
    }

    private async UniTask<bool> RunAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions()
                    .SetEnvironmentName(Debug.isDebugBuild ? DevelopmentEnvironment : ProductionEnvironment);
                await UnityServices.InitializeAsync(options);
            }

            return true;
        }
        catch (Exception e)
        {
            started = false;
            Debug.LogWarning($"[UGS] Initialization failed: {e.Message}");
            return false;
        }
    }
}
