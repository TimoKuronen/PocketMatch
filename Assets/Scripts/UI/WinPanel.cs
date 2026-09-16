using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Win panel menu that appears when player completes a level.
/// Acts as a View in the MVP pattern.
/// </summary>
public class WinPanel : UIMenu, IWinView
{
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private ConfirmationDialog confirmationDialog;

    private ILocalizationService localization;
    private int cachedEarnedCoins;
    private bool hasEarnedCoins;

    public event System.Action NextLevelClicked;
    public event System.Action MainMenuClicked;

    [Inject]
    public void Construct(ILocalizationService localization)
    {
        this.localization = localization;
        localization.LocaleChanged += OnLocaleChanged;
    }

    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.WinMenu;

        nextLevelButton.onClick.AddListener(() => NextLevelClicked?.Invoke());
        mainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
    }

    protected override void OnDestroy()
    {
        nextLevelButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        if (localization != null)
            localization.LocaleChanged -= OnLocaleChanged;
        base.OnDestroy();
    }

    public void SetEarnedCoins(int coins)
    {
        cachedEarnedCoins = coins;
        hasEarnedCoins = true;

        if (localization == null || !localization.IsReady)
        {
            coinCountText.text = $"+{coins} coins earned";
            return;
        }

        coinCountText.text = localization.Get(LocalizationKeys.WinCoinsEarned, coins);
    }

    public void SetNextLevelButtonVisible(bool isVisible)
    {
        nextLevelButton.gameObject.SetActive(isVisible);
    }

    private void OnLocaleChanged()
    {
        if (hasEarnedCoins)
            SetEarnedCoins(cachedEarnedCoins);
    }
}
