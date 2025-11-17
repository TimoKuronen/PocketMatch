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
        this.saveService = saveService;
        this.adsService = adsService;
    }

    private void Start()
    {
        levelPanel.SetActive(true);
        settingsPanel.SetActive(false);
        levelIndex = saveService.PlayerData.nextLevelIndex;
        LoadInitialValues();
        StartCoroutine(ShowBannerWhenReady());
    }

    private IEnumerator ShowBannerWhenReady()
    {
        yield return new WaitUntil(() => adsService.IsInitialized);
        yield return new WaitForSeconds(0.5f);
        adsService.ShowBannerAd();
    }

    private void LoadInitialValues()
    {
        coinCountText.text = "x " + saveService.PlayerData.coins.ToString();
        levelText.text = "Level " + (levelIndex + 1).ToString();
    }

    public void PlayButtonPressed()
    {
        adsService.HideBannerAd();
        Loader.Load(Loader.GameScene.PlayScene);
    }

    public void SettingsButtonPressed() { }

    public void ResetSaveButtonPressed()
    {
        saveService.ResetToDefaults();
        levelIndex = saveService.PlayerData.nextLevelIndex;
        LoadInitialValues();
    }
}
