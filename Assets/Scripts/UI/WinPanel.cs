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
    
    public event System.Action NextLevelClicked;
    public event System.Action MainMenuClicked;

    [Inject]
    public void Construct() { }
    
    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.WinMenu;
        
        // Subscribe to button clicks via code
        nextLevelButton.onClick.AddListener(() => NextLevelClicked?.Invoke());
        mainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
    }
    
    protected override void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        nextLevelButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        base.OnDestroy();
    }
    
    public override void Open()
    {
        base.Open();
    }

    public void SetEarnedCoins(int coins)
    {
        coinCountText.text = $"+{coins} coins earned";
    }

    public void SetNextLevelButtonVisible(bool isVisible)
    {
        nextLevelButton.gameObject.SetActive(isVisible);
    }
}
