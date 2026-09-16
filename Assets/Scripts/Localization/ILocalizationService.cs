using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Thin lookup seam over Unity Localization so presenters are not tied to LocalizationSettings calls.
/// </summary>
public interface ILocalizationService
{
    event Action LocaleChanged;

    bool IsReady { get; }

    string Get(string key);

    string Get(string key, params object[] args);

    UniTask WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}
