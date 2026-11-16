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
    private Action onCancelCallback;
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

        // The message text is set per dialog; a bound label would overwrite it with the generic prompt.
        var boundLabel = messageText.GetComponent<LocalizedTmpLabel>();
        if (boundLabel != null)
            boundLabel.enabled = false;
        
        // Subscribe to button clicks via code
        yesButton.onClick.AddListener(OnYesButtonClicked);
        noButton.onClick.AddListener(OnNoButtonClicked);
    }
    
    protected override void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        yesButton.onClick.RemoveListener(OnYesButtonClicked);
        noButton.onClick.RemoveListener(OnNoButtonClicked);
        base.OnDestroy();
    }
    
    /// <summary>
    /// Set up the confirmation dialog with a message and callbacks for each answer.
    /// </summary>
    public void Setup(string message, Action onConfirm, Action onCancel = null)
    {
        messageText.text = message;
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
    }
    
    private void OnYesButtonClicked()
    {
        onConfirmCallback?.Invoke();
        menuStackManager.PopMenu();
    }
    
    private void OnNoButtonClicked()
    {
        onCancelCallback?.Invoke();
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