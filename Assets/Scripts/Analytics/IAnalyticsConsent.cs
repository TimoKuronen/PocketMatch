using System;

/// <summary>Device-level analytics consent. Not part of the synced player save.</summary>
public interface IAnalyticsConsent
{
    AnalyticsConsentState State { get; }

    event Action StateChanged;

    void SetGranted(bool granted);
}
