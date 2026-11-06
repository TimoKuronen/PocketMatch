using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// IAdsService facade. Loads the local LevelPlay config, starts SDK init, and delegates each ad format
/// to its controller. Controllers exist even without a valid config so Editor simulation still works.
/// </summary>
public class AdsService : IAdsService, IDisposable
{
    private const string ConfigFileName = "levelplay-config.json";
    // Lets the SDK settle after init success before ad objects are created.
    private const float AdCreationDelaySeconds = 0.5f;

    private readonly CancellationTokenSource cts = new();
    private readonly AdsLogger log = new(Application.isEditor || Debug.isDebugBuild);

    private LevelPlaySdkInitializer sdk;
    private BannerAdController banner;
    private InterstitialAdController interstitial;
    private RewardedAdController rewarded;

    public event Action OnInterstitialAdClosed;

    public bool IsInitialized => sdk?.IsInitialized ?? false;
    public bool InterstitialAdReady => interstitial?.IsReady ?? false;
    public bool InterstitialAdCompleted => interstitial?.Completed ?? false;
    public bool IsRewardedAdAvailable => rewarded?.IsAvailable ?? false;

    [Inject]
    public void Construct(IAnalyticsService analyticsService)
    {
        bool hasConfig = TryLoadConfig(out LevelPlayConfig config);
        var analytics = new AdsAnalytics(analyticsService, log);
        Func<bool> isSdkReady = () => IsInitialized;
        var token = cts.Token;

        banner = new BannerAdController(config.bannerAdId, isSdkReady, log, token);
        interstitial = new InterstitialAdController(config.interstitialAdId, isSdkReady, banner.Hide, analytics, log, token);
        rewarded = new RewardedAdController(config.rewardedAdId, isSdkReady, analytics, log, token);
        interstitial.Closed += () => OnInterstitialAdClosed?.Invoke();

        if (!hasConfig)
            return;

        sdk = new LevelPlaySdkInitializer(config.appKey, log, token);
        sdk.Initialized += OnSdkInitialized;
        sdk.Start();
    }

    public void ShowBannerAd() => banner.Show();

    public void HideBannerAd() => banner.Hide();

    public void ShowInterstitialAd() => interstitial.Show();

    public void ForceMarkAdComplete() => interstitial.MarkComplete();

    public UniTask<RewardedAdResult> ShowRewardedAdAsync(CancellationToken cancellationToken = default) =>
        rewarded.ShowAsync(cancellationToken);

    public void Dispose()
    {
        interstitial?.Dispose();
        banner?.Dispose();
        rewarded?.Dispose();
        sdk?.Dispose();

        if (!cts.IsCancellationRequested)
            cts.Cancel();
        cts.Dispose();
    }

    private bool TryLoadConfig(out LevelPlayConfig config)
    {
        if (!LocalJsonConfig.TryLoad(ConfigFileName, out config) || config == null || !config.IsValid)
        {
            log.Warn($"LevelPlay config missing or incomplete; ads stay disabled until Assets/StreamingAssets/{ConfigFileName} is present.");
            config = new LevelPlayConfig
            {
                appKey = string.Empty,
                bannerAdId = string.Empty,
                interstitialAdId = string.Empty,
                rewardedAdId = string.Empty
            };
            return false;
        }

        config.appKey = config.appKey.Trim();
        config.bannerAdId = config.bannerAdId.Trim();
        config.interstitialAdId = config.interstitialAdId.Trim();
        config.rewardedAdId = config.HasRewardedAdId ? config.rewardedAdId.Trim() : string.Empty;

        if (!config.HasRewardedAdId)
            log.Warn("No rewardedAdId in LevelPlay config; rewarded continue stays unavailable on device.");

        return true;
    }

    private void OnSdkInitialized()
    {
        log.Verbose($"State after init: {Describe()}");
        CreateAdsAfterDelayAsync().Forget();
    }

    private async UniTaskVoid CreateAdsAfterDelayAsync()
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(AdCreationDelaySeconds), cancellationToken: cts.Token)
            .SuppressCancellationThrow();

        if (cancelled)
            return;

        banner.CreateAd();
        interstitial.CreateAd();
        rewarded.CreateAd();
        log.Verbose($"State after ad creation: {Describe()}");
    }

    private string Describe() =>
        $"initialized={IsInitialized}, internet={Application.internetReachability}, " +
        $"{banner.Describe()}, {interstitial.Describe()}, {rewarded.Describe()}";
}
