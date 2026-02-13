using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Reusable confirmation dialog that can be pushed onto the menu stack.
/// </summary>
public class ConfirmationDialog : UIMenu
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    
    private Action onConfirmCallback;
    private MenuStackManager menuStackManager;
    
    [Inject]
    public void Construct(MenuStackManager menuStackManager)
    {
        this.menuStackManager = menuStackManager;
    }
    
    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.ConfirmationPrompt;
        
        // Subscribe to button clicks via code
        yesButton.onClick.AddListener(OnYesButtonClicked);
        noButton.onClick.AddListener(OnNoButtonClicked);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        yesButton.onClick.RemoveListener(OnYesButtonClicked);
        noButton.onClick.RemoveListener(OnNoButtonClicked);
    }
    
    /// <summary>
    /// Set up the confirmation dialog with a message and callback
    /// </summary>
    public void Setup(string message, Action onConfirm)
    {
        messageText.text = message;
        onConfirmCallback = onConfirm;
    }
    
    private void OnYesButtonClicked()
    {
        onConfirmCallback?.Invoke();
        menuStackManager.PopMenu();
    }
    
    private void OnNoButtonClicked()
    {
        menuStackManager.PopMenu();
    }
    
    public override void Open()
    {
        base.Open();
        
        // Ensure buttons are enabled
        yesButton.interactable = true;
        noButton.interactable = true;
    }
}
