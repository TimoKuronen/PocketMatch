using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Mediation;
using System;
using System.Threading.Tasks;

public class RewardedAdManager : MonoBehaviour
{
    [Header("Unity Mediation IDs")]
    [SerializeField] private string androidAdUnitId = "Rewarded_Android";
    [SerializeField] private string iOSAdUnitId = "Rewarded_iOS";

    private IRewardedAd rewardedAd;
    private string adUnitId;

    private async void Start()
    {
        await InitializeServices();
        SetupRewardedAd();
    }

    private async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services failed to initialize: {e}");
        }
    }

    private void SetupRewardedAd()
    {
#if UNITY_ANDROID
        adUnitId = androidAdUnitId;
#elif UNITY_IOS
        adUnitId = iOSAdUnitId;
#else
        Debug.LogWarning("Unsupported platform for ads");
        return;
#endif

        rewardedAd = MediationService.Instance.CreateRewardedAd(adUnitId);

        // Subscribe to events
        rewardedAd.OnLoaded += OnAdLoaded;
        rewardedAd.OnFailedLoad += OnAdFailedLoad;
        rewardedAd.OnClosed += OnAdClosed;
        rewardedAd.OnUserRewarded += OnUserRewarded;

        // Load first ad
        LoadAd();
    }

    public void LoadAd()
    {
        if (rewardedAd.AdState == AdState.Loaded) 
            return;

        Debug.Log("Loading Rewarded Ad...");
        rewardedAd.LoadAsync();
    }

    public void ShowAd()
    {
        if (rewardedAd.AdState == AdState.Loaded)
        {
            Debug.Log("Showing Rewarded Ad...");
            rewardedAd.ShowAsync();
        }
        else
        {
            Debug.LogWarning("Rewarded Ad not ready yet.");
            LoadAd();
        }
    }

    private void OnAdLoaded(object sender, EventArgs e)
    {
        Debug.Log("Rewarded Ad loaded successfully.");
    }

    private void OnAdFailedLoad(object sender, LoadErrorEventArgs e)
    {
        Debug.LogError($"Rewarded Ad failed to load: {e.Message}");
    }

    private void OnAdClosed(object sender, EventArgs e)
    {
        Debug.Log("Rewarded Ad closed. Reloading...");
        LoadAd();
    }

    private void OnUserRewarded(object sender, RewardEventArgs e)
    {
        Debug.Log($"User rewarded! Type: {e.Type}, Amount: {e.Amount}");
    }
}
