using System;
using UnityEngine;

public abstract class UIMenu : MonoBehaviour, IMenu
{
    [SerializeField] protected GameObject menuPanel;
    [SerializeField] protected MenuType menuType;
    
    public MenuType MenuType => menuType;
    public bool IsOpen { get; protected set; }
    
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;
    public event Action OnButtonPressed;
    
    protected virtual void Awake()
    {
        if (menuPanel == null)
        {
            menuPanel = gameObject;
        }
        
        // Start with menu closed
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }
    
    public virtual void Open()
    {
        if (IsOpen)
            return;
            
        IsOpen = true;
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        OnMenuOpened?.Invoke();
    }
    
    public virtual void Close()
    {
        if (!IsOpen)
            return;
            
        IsOpen = false;
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        OnMenuClosed?.Invoke();
    }
    
    public virtual bool CanOpen()
    {
        return true; // Override in derived classes if needed
    }
}
