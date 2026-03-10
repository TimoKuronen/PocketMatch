using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Main menu panel displayed in the main menu scene.
/// </summary>
public class MainMenuPanel : UIMenu
{
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button resetSaveButton;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    
    private ISaveService saveService;
    private IAdsService adsService;
    private MenuStackManager menuStackManager;
    private int levelIndex;
    
    [Inject]
    public void Construct(ISaveService saveService, IAdsService adsService, MenuStackManager menuStackManager)
    {
        this.saveService = saveService;
        this.adsService = adsService;
        this.menuStackManager = menuStackManager;
    }
    
    protected override void Awake()
    {
        // Don't call base.Awake() - we want the main menu to be visible by default
        if (menuPanel == null)
        {
            menuPanel = gameObject;
        }
        
        menuType = MenuType.PauseMenu;
        
        // Main menu should be open by default
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        IsOpen = true;
        
        // Subscribe to button clicks via code
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        resetSaveButton.onClick.AddListener(OnResetSaveButtonClicked);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
        settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        resetSaveButton.onClick.RemoveListener(OnResetSaveButtonClicked);
    }
    
    private void Start()
    {
        levelIndex = saveService.PlayerData.nextLevelIndex;
        LoadInitialValues();
        ShowBannerWhenReadyAsync().Forget();
    }
    
    private async UniTaskVoid ShowBannerWhenReadyAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.WaitUntil(() => adsService.IsInitialized, cancellationToken: token);
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
        adsService.ShowBannerAd();
    }
    
    private void LoadInitialValues()
    {
        coinCountText.text = $"x {saveService.PlayerData.coins}";
        levelText.text = $"Level {levelIndex + 1}";
    }
    
    private void OnPlayButtonClicked()
    {
        adsService.HideBannerAd();
        Loader.Load(Loader.GameScene.PlayScene);
    }
    
    private void OnSettingsButtonClicked()
    {
        // Toggle settings menu - close if already open, open if closed
        if (menuStackManager.HasMenuOfType(MenuType.SettingsMenu))
        {
            menuStackManager.PopMenuOfType(MenuType.SettingsMenu);
        }
        else
        {
            if (menuStackManager.CanOpenMenu())
            {
                menuStackManager.PushMenu(settingsPanel);
            }
        }
    }
    
    private void OnResetSaveButtonClicked()
    {
        saveService.ResetToDefaults();
        levelIndex = saveService.PlayerData.nextLevelIndex;
        LoadInitialValues();
    }
}
