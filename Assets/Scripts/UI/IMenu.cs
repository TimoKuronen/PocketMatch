using System;

public interface IMenu
{
    MenuType MenuType { get; }
    bool IsOpen { get; }
    
    /// <summary>
    /// Called when menu is pushed onto the stack
    /// </summary>
    void Open();
    
    /// <summary>
    /// Called when menu is popped from the stack
    /// </summary>
    void Close();
    
    /// <summary>
    /// Check if this menu can be opened (e.g., not during match processing)
    /// </summary>
    bool CanOpen();
    
    /// <summary>
    /// Event fired when menu is opened
    /// </summary>
    event Action OnMenuOpened;
    
    /// <summary>
    /// Event fired when menu is closed
    /// </summary>
    event Action OnMenuClosed;
}
