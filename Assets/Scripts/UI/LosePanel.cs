using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LosePanel : UIMenu, ILoseView
{
    private static readonly Color UnavailableLabelColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button continueWithCoinsButton;
    [SerializeField] private Button continueWithAdButton;
    [SerializeField] private TextMeshProUGUI walletBalanceText;
    [SerializeField] private TextMeshProUGUI continueWithCoinsLabel;
    [SerializeField] private TextMeshProUGUI continueWithAdLabel;
    [SerializeField] private ConfirmationDialog confirmationDialog;

    private Color continueWithCoinsDefaultColor;
    private FontStyles continueWithCoinsDefaultFontStyle;
    private Color continueWithAdDefaultColor;
    private FontStyles continueWithAdDefaultFontStyle;

    public event Action RestartClicked;
    public event Action MainMenuClicked;
    public event Action ContinueWithCoinsClicked;
    public event Action ContinueWithAdClicked;
    public event Action Opened;

    [Inject]
    public void Construct() { }

    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.LoseMenu;
        CacheLabelDefaults();
        ValidateReferences();

        restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        mainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
        continueWithCoinsButton.onClick.AddListener(() => ContinueWithCoinsClicked?.Invoke());
        continueWithAdButton.onClick.AddListener(() => ContinueWithAdClicked?.Invoke());
    }

    public override void Open()
    {
        base.Open();
        Opened?.Invoke();
    }

    public void SetWalletBalance(int balance)
    {
        walletBalanceText.text = $"x {balance}";
    }

    public void SetContinueWithCoinsAvailable(bool isAvailable)
    {
        ApplyContinueLabelState(continueWithCoinsButton, continueWithCoinsLabel,
            continueWithCoinsDefaultColor, continueWithCoinsDefaultFontStyle, isAvailable);
    }

    public void SetContinueWithAdAvailable(bool isAvailable)
    {
        ApplyContinueLabelState(continueWithAdButton, continueWithAdLabel,
            continueWithAdDefaultColor, continueWithAdDefaultFontStyle, isAvailable);
    }

    private void CacheLabelDefaults()
    {
        if (continueWithCoinsLabel != null)
        {
            continueWithCoinsDefaultColor = continueWithCoinsLabel.color;
            continueWithCoinsDefaultFontStyle = continueWithCoinsLabel.fontStyle;
        }

        if (continueWithAdLabel != null)
        {
            continueWithAdDefaultColor = continueWithAdLabel.color;
            continueWithAdDefaultFontStyle = continueWithAdLabel.fontStyle;
        }
    }

    private static void ApplyContinueLabelState(
        Button button,
        TextMeshProUGUI label,
        Color defaultColor,
        FontStyles defaultFontStyle,
        bool isAvailable)
    {
        button.interactable = isAvailable;

        if (isAvailable)
        {
            label.color = defaultColor;
            label.fontStyle = defaultFontStyle;
            return;
        }

        label.color = UnavailableLabelColor;
        label.fontStyle = FontStyles.Italic;
    }

    private bool HasValidReferences()
    {
        return restartButton != null
            && mainMenuButton != null
            && continueWithCoinsButton != null
            && continueWithAdButton != null
            && walletBalanceText != null
            && continueWithCoinsLabel != null
            && continueWithAdLabel != null
            && confirmationDialog != null;
    }

    private void ValidateReferences()
    {
        if (HasValidReferences())
            return;

        Debug.LogError("[LosePanel] Required UI references are not configured on GameplayCanvas > LosePanel.");
    }

    protected override void OnDestroy()
    {
        restartButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        continueWithCoinsButton.onClick.RemoveAllListeners();
        continueWithAdButton.onClick.RemoveAllListeners();
        base.OnDestroy();
    }
}
