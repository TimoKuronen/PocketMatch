using System.Collections;
using TMPro;
using UnityEngine;
using VContainer;

public class UI_MainMenu : UIMenu
{
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI levelText;

    private ISaveService saveService;
    private IAdsService adsService;
    private int levelIndex;

    [Inject]
    public void Construct(ISaveService saveService, IAdsService adsService)
    {
        Debug.Log("UI_MainMenu Construct called with services " + saveService + ", " + adsService);
        this.saveService = saveService;
        this.adsService = adsService;
    }

    private void Start()
    {
        Debug.Log("UI_MainMenu Start called");
        levelPanel.SetActive(true);
        settingsPanel.SetActive(false);

        levelIndex = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();

        adsService.ShowBannerAd();
    }

    private void LoadInitialValues()
    {
        coinCountText.text = "x " + saveService.PlayerData.coins.ToString();
        levelText.text = "Level " + (levelIndex + 1).ToString();
    }

    public void PlayButtonPressed()
    {
        Debug.Log("Play Button pressed, loading level " + (levelIndex + 1));
        adsService.HideBannerAd();
        Loader.Load(Loader.GameScene.PlayScene);
    }

    private IEnumerator HandleLevelLoadingWithAd()
    {
        StartCoroutine(Loader.ShowInterstitialThenContinue(adsService, Loader.GameScene.PlayScene));

        Debug.Log("Waiting for ad to complete...");

        yield return new WaitUntil(() => adsService.InterstitialAdCompleted);

        Loader.Load(Loader.GameScene.PlayScene);
        Debug.Log("Ad completed, loading next level...");
    }

    public void SettingsButtonPressed()
    {
        Debug.Log("Settings Button pressed, not yet implemented");
    }

    public void ResetSaveButtonPressed()
    {
        saveService.ResetToDefaults();

        levelIndex = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();
    }
}
