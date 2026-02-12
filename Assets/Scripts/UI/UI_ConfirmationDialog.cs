using System;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// Reusable confirmation dialog that can be pushed onto the menu stack.
/// </summary>
public class UI_ConfirmationDialog : UIMenu
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private UnityEngine.UI.Button yesButton;
    [SerializeField] private UnityEngine.UI.Button noButton;
    
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
    }
    
    /// <summary>
    /// Set up the confirmation dialog with a message and callback
    /// </summary>
    public void Setup(string message, Action onConfirm)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        onConfirmCallback = onConfirm;
    }
    
    public void YesButtonPressed()
    {
        onConfirmCallback?.Invoke();
        
        if (menuStackManager != null)
        {
            menuStackManager.PopMenu();
        }
    }
    
    public void NoButtonPressed()
    {
        if (menuStackManager != null)
        {
            menuStackManager.PopMenu();
        }
    }
    
    public override void Open()
    {
        base.Open();
        
        // Ensure buttons are enabled
        if (yesButton != null) yesButton.interactable = true;
        if (noButton != null) noButton.interactable = true;
    }
}
