using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// Manages a stack of menus, ensuring proper layering and preventing menu opening during match processing.
/// </summary>
public class MenuStackManager : IDisposable
{
    private Stack<IMenu> menuStack = new Stack<IMenu>();
    private IGridController gridController;
    
    public bool HasOpenMenus => menuStack.Count > 0;
    public IMenu TopMenu => menuStack.Count > 0 ? menuStack.Peek() : null;
    
    [Inject]
    public void Construct(IGridController gridController)
    {
        this.gridController = gridController;
    }
    
    /// <summary>
    /// Check if a menu can be opened (not during match processing)
    /// </summary>
    public bool CanOpenMenu()
    {
        return gridController != null && !gridController.IsProcessingTiles;
    }
    
    /// <summary>
    /// Push a menu onto the stack and open it
    /// </summary>
    public bool PushMenu(IMenu menu)
    {
        if (menu == null)
        {
            Debug.LogWarning("[MenuStackManager] Attempted to push null menu");
            return false;
        }
        
        if (!CanOpenMenu())
        {
            Debug.LogWarning("[MenuStackManager] Cannot open menu - matches are being processed");
            return false;
        }
        
        if (!menu.CanOpen())
        {
            Debug.LogWarning($"[MenuStackManager] Menu {menu.MenuType} cannot be opened");
            return false;
        }
        
        // Close current top menu if exists (for visual layering)
        if (menuStack.Count > 0)
        {
            var topMenu = menuStack.Peek();
            // Don't close it, just hide visually if needed
        }
        
        menuStack.Push(menu);
        menu.Open();
        
        Debug.Log($"[MenuStackManager] Pushed menu: {menu.MenuType}. Stack size: {menuStack.Count}");
        return true;
    }
    
    /// <summary>
    /// Pop the top menu from the stack and close it
    /// </summary>
    public bool PopMenu()
    {
        if (menuStack.Count == 0)
        {
            Debug.LogWarning("[MenuStackManager] Attempted to pop from empty stack");
            return false;
        }
        
        var menu = menuStack.Pop();
        menu.Close();
        
        Debug.Log($"[MenuStackManager] Popped menu: {menu.MenuType}. Stack size: {menuStack.Count}");
        return true;
    }
    
    /// <summary>
    /// Pop all menus from the stack
    /// </summary>
    public void PopAllMenus()
    {
        while (menuStack.Count > 0)
        {
            PopMenu();
        }
    }
    
    /// <summary>
    /// Close all menus and clear the stack (useful when leaving level)
    /// </summary>
    public void ClearStack()
    {
        while (menuStack.Count > 0)
        {
            var menu = menuStack.Pop();
            menu.Close();
        }
    }
    
    public void Dispose()
    {
        ClearStack();
    }
}
