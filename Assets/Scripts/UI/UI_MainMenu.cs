using System.Collections;
using TMPro;
using UnityEngine;

public class UI_MainMenu : UIMenu
{
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI levelText;

    private ISaveService saveService;
    private IAdsManager adsManager;
    private int levelIndex;

    private void Start()
    {
        levelPanel.SetActive(true);
        settingsPanel.SetActive(false);

        saveService = Services.Get<ISaveService>();
        adsManager = Services.Get<IAdsManager>();

        levelIndex = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();

        adsManager.ShowBannerAd();
    }

    private void LoadInitialValues()
    {
        coinCountText.text = "x " + saveService.PlayerData.coins.ToString();
        levelText.text = "Level " + (levelIndex + 1).ToString();
    }

    public void PlayButtonPressed()
    {
        Debug.Log("Play Button pressed, loading level " + (levelIndex + 1));
        adsManager.HideBannerAd();
        StartCoroutine(Loader.CallDelayedLoad(Loader.Scene.PlayScene));
    }

    private IEnumerator HandleLevelLoadingWithAd()
    {
        StartCoroutine(Loader.ShowInterstitialThenContinue(adsManager, Loader.Scene.PlayScene));

        Debug.Log("Waiting for ad to complete...");

        yield return new WaitUntil(() => adsManager.InterstitialAdCompleted);

        StartCoroutine(Loader.CallDelayedLoad(Loader.Scene.PlayScene));
        Debug.Log("Ad completed, loading next level...");
    }

    public void SettingsButtonPressed()
    {
        Debug.Log("Settings Button pressed, not yet implemented");
    }

    public void ResetSaveButtonPressed()
    {
        Services.Get<ISaveService>().ResetToDefaults();

        levelIndex = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();
    }

    public void AdsButtonPressed()
    {
        Services.Get<IAdsManager>().ShowBannerAd();
    }
}
