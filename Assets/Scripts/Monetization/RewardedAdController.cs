using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;

/// <summary>
/// Rewarded lifecycle and show outcome. Every failure path resolves the pending show as
/// <see cref="RewardedAdResult.Unavailable"/> instead of throwing.
/// </summary>
public sealed class RewardedAdController : IDisposable
{
    private const string AdFormat = "rewarded";
    // Analytics label only; the SDK show uses the ad unit's default placement.
    private const string PlacementName = "lose_continue";
    // Some networks fire the reward callback just after close; wait briefly before calling it a dismissal.
    private const float RewardAfterCloseGraceSeconds = 1f;

    private readonly string adUnitId;
    private readonly Func<bool> isSdkReady;
    private readonly AdsAnalytics analytics;
    private readonly AdsLogger log;
    private readonly CancellationToken token;
    private readonly AdsRetryPolicy retryPolicy = new();

    private LevelPlayRewardedAd rewardedAd;
    private UniTaskCompletionSource<RewardedAdResult> pendingShow;
    private bool rewardEarned;
    private bool closed;
    private bool lastLoadWasNoFill;

#if UNITY_EDITOR
    public bool IsAvailable => true;
#else
    public bool IsAvailable => isSdkReady() && rewardedAd != null;
#endif

    public RewardedAdController(
        string adUnitId,
        Func<bool> isSdkReady,
        AdsAnalytics analytics,
        AdsLogger log,
        CancellationToken token)
    {
        this.adUnitId = adUnitId;
        this.isSdkReady = isSdkReady;
        this.analytics = analytics;
        this.log = log;
        this.token = token;
    }

    public string Describe() =>
        $"Rewarded(configured={!string.IsNullOrEmpty(adUnitId)}, hasAd={rewardedAd != null}, ready={rewardedAd?.IsAdReady() ?? false})";

    public void CreateAd()
    {
        if (!isSdkReady() || string.IsNullOrEmpty(adUnitId))
            return;

        Cleanup();

        try
        {
            rewardedAd = new LevelPlayRewardedAd(adUnitId);
            rewardedAd.OnAdLoaded += OnLoaded;
            rewardedAd.OnAdLoadFailed += OnLoadFailed;
            rewardedAd.OnAdDisplayed += OnDisplayed;
            rewardedAd.OnAdDisplayFailed += OnDisplayFailed;
            rewardedAd.OnAdRewarded += OnRewarded;
            rewardedAd.OnAdClosed += OnClosed;

            Load();
        }
        catch (Exception e)
        {
            log.Error($"Rewarded creation threw: {e}");
        }
    }

    public UniTask<RewardedAdResult> ShowAsync(CancellationToken cancellationToken)
    {
#if UNITY_EDITOR
        log.Verbose("Simulating rewarded completion in Editor.");
        analytics.Watched(AdFormat, PlacementName, "editor_simulated");
        return UniTask.FromResult(RewardedAdResult.Rewarded);
#else
        if (pendingShow != null)
        {
            log.Warn("Rewarded show ignored: one is already showing.");
            return UniTask.FromResult(RewardedAdResult.Unavailable);
        }

        if (!isSdkReady() || rewardedAd == null)
        {
            LogSkipped(string.IsNullOrEmpty(adUnitId) ? "not_configured" : "not_initialized");
            return UniTask.FromResult(RewardedAdResult.Unavailable);
        }

        if (!rewardedAd.IsAdReady())
        {
            LogSkipped(lastLoadWasNoFill ? "no_fill" : "not_ready");
            retryPolicy.Reset();
            Load();
            return UniTask.FromResult(RewardedAdResult.Unavailable);
        }

        pendingShow = new UniTaskCompletionSource<RewardedAdResult>();
        rewardEarned = false;
        closed = false;

        rewardedAd.ShowAd();
        return pendingShow.Task.AttachExternalCancellation(cancellationToken);
#endif
    }

    private void Load()
    {
        if (rewardedAd == null)
            return;

        log.Verbose("Requesting rewarded load.");
        rewardedAd.LoadAd();
    }

    private void OnLoaded(LevelPlayAdInfo adInfo)
    {
        retryPolicy.Reset();
        lastLoadWasNoFill = false;
        log.Verbose($"Rewarded loaded. {adInfo}");
    }

    private void OnLoadFailed(LevelPlayAdError error)
    {
        var kind = AdsRetryPolicy.Classify(error.ToString());
        lastLoadWasNoFill = kind == AdsRetryPolicy.FailureKind.NoFill;
        log.Warn($"Rewarded load failed ({kind}): {error}");

        if (!retryPolicy.TryGetRetryDelay(kind, out float delay))
        {
            log.Warn("Rewarded load will not retry; the next show attempt requests a new load.");
            return;
        }

        RetryLoadAsync(delay).Forget();
    }

    private async UniTaskVoid RetryLoadAsync(float delay)
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(delay), DelayType.Realtime, cancellationToken: token)
            .SuppressCancellationThrow();

        if (!cancelled && rewardedAd != null && !rewardedAd.IsAdReady())
            Load();
    }

    private void OnDisplayed(LevelPlayAdInfo adInfo)
    {
        log.Verbose($"Rewarded displayed. {adInfo}");
    }

    private void OnDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        log.Error($"Rewarded display failed: {error}");
        LogSkipped("display_failed");
        CompleteShow(RewardedAdResult.Unavailable);
        Load();
    }

    private void OnRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        log.Verbose($"Rewarded granted: {reward?.Name} x{reward?.Amount}");
        rewardEarned = true;

        if (closed)
            FinishShow();
    }

    private void OnClosed(LevelPlayAdInfo adInfo)
    {
        log.Verbose($"Rewarded closed. rewardEarned={rewardEarned}");
        closed = true;

        if (rewardEarned)
            FinishShow();
        else
            FinishShowAfterGraceAsync(pendingShow).Forget();
    }

    private async UniTaskVoid FinishShowAfterGraceAsync(UniTaskCompletionSource<RewardedAdResult> show)
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(RewardAfterCloseGraceSeconds), DelayType.Realtime, cancellationToken: token)
            .SuppressCancellationThrow();

        if (!cancelled && show != null && pendingShow == show)
            FinishShow();
    }

    private void FinishShow()
    {
        if (pendingShow == null)
            return;

        analytics.Watched(AdFormat, PlacementName, rewardEarned ? "rewarded" : "dismissed");
        CompleteShow(rewardEarned ? RewardedAdResult.Rewarded : RewardedAdResult.Dismissed);
        Load();
    }

    private void CompleteShow(RewardedAdResult result)
    {
        var show = pendingShow;
        pendingShow = null;
        show?.TrySetResult(result);
    }

    private void LogSkipped(string reason)
    {
        log.Warn($"Rewarded ad unavailable: {reason}");
        analytics.Skipped(AdFormat, PlacementName, reason);
    }

    private void Cleanup()
    {
        if (rewardedAd == null)
            return;

        rewardedAd.OnAdLoaded -= OnLoaded;
        rewardedAd.OnAdLoadFailed -= OnLoadFailed;
        rewardedAd.OnAdDisplayed -= OnDisplayed;
        rewardedAd.OnAdDisplayFailed -= OnDisplayFailed;
        rewardedAd.OnAdRewarded -= OnRewarded;
        rewardedAd.OnAdClosed -= OnClosed;
        rewardedAd = null;
    }

    public void Dispose()
    {
        Cleanup();
        CompleteShow(RewardedAdResult.Unavailable);
    }
}
