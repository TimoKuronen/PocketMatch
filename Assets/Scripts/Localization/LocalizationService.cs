using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using VContainer.Unity;

/// <summary>
/// Boots Unity Localization and exposes string lookup.
/// Avoids WaitForCompletion inside Addressables callbacks (reentrancy).
/// </summary>
public sealed class LocalizationService : ILocalizationService, IStartable, IDisposable
{
    public event Action LocaleChanged;

    private bool isReady;
    private bool initialized;
    private bool disposed;

    public bool IsReady => isReady;

    public void Start()
    {
        if (!LocalizationSettings.HasSettings)
        {
            Debug.LogWarning("[Localization] No Localization Settings asset. Run PocketMatch > Localization > Create Foundation.");
            return;
        }

        CompleteInitializationAsync().Forget();
    }

    public string Get(string key)
    {
        if (!CanReadTables())
            return key;

        return LocalizationSettings.StringDatabase.GetLocalizedString(LocalizationKeys.UiTable, key);
    }

    public string Get(string key, params object[] args)
    {
        if (!CanReadTables())
            return key;

        return LocalizationSettings.StringDatabase.GetLocalizedString(LocalizationKeys.UiTable, key, args);
    }

    public async UniTask WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        await UniTask.WaitUntil(() => isReady || disposed, cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        disposed = true;
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private bool CanReadTables()
    {
        return isReady && LocalizationSettings.HasSettings;
    }

    private async UniTaskVoid CompleteInitializationAsync()
    {
        if (initialized || disposed)
            return;

        initialized = true;

        var init = LocalizationSettings.InitializationOperation;
        await UniTask.WaitUntil(() => init.IsDone || disposed);
        if (disposed)
            return;

        // Leave Addressables/ResourceManager callback stack before sync loads or locale changes.
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (disposed)
            return;

        var tableHandle = LocalizationSettings.StringDatabase.GetTableAsync(LocalizationKeys.UiTable);
        await UniTask.WaitUntil(() => tableHandle.IsDone || disposed);
        if (disposed)
            return;

        await UniTask.Yield(PlayerLoopTiming.Update);
        if (disposed)
            return;

        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        isReady = true;
        LocaleChanged?.Invoke();
    }

    private void OnSelectedLocaleChanged(Locale _)
    {
        if (!isReady || disposed)
            return;

        NotifyLocaleChangedDeferred().Forget();
    }

    private async UniTaskVoid NotifyLocaleChangedDeferred()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (disposed || !isReady)
            return;

        var tableHandle = LocalizationSettings.StringDatabase.GetTableAsync(LocalizationKeys.UiTable);
        await UniTask.WaitUntil(() => tableHandle.IsDone || disposed);
        if (disposed || !isReady)
            return;

        LocaleChanged?.Invoke();
    }
}
