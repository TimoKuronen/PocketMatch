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
    
    /// <summary>
    /// Check if a menu of the specified type is currently on the stack
    /// </summary>
    public bool HasMenuOfType(MenuType menuType)
    {
        foreach (var menu in menuStack)
        {
            if (menu.MenuType == menuType)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Pop menus until the specified menu type is removed (if it exists)
    /// Closes all menus popped in the process, including the target menu
    /// </summary>
    public bool PopMenuOfType(MenuType menuType)
    {
        if (!HasMenuOfType(menuType))
            return false;
        
        // Pop menus until we find and remove the target menu
        // Close everything we pop, including menus above the target
        var menusToRestore = new Stack<IMenu>();
        bool found = false;
        
        while (menuStack.Count > 0)
        {
            var menu = menuStack.Pop();
            menu.Close();
            
            if (menu.MenuType == menuType)
            {
                found = true;
                break; // Stop here, don't restore menus above settings
            }
            
            menusToRestore.Push(menu);
        }
        
        // Restore menus that were below the target (they were closed, so reopen them)
        while (menusToRestore.Count > 0)
        {
            var menu = menusToRestore.Pop();
            menuStack.Push(menu);
            menu.Open();
        }
        
        return found;
    }
    
    [Inject]
    public void Construct() { }
    
    /// <summary>
    /// Set grid controller reference (called from GameLifetimeScope)
    /// </summary>
    public void SetGridController(IGridController gridController)
    {
        this.gridController = gridController;
    }
    
    /// <summary>
    /// Check if a menu can be opened (not during match processing)
    /// </summary>
    public bool CanOpenMenu()
    {
        // If no grid controller (main menu scene), always allow opening menus
        if (gridController == null)
            return true;
            
        return !gridController.IsProcessingTiles;
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
        
        // Ensure menu GameObject is enabled before opening (Awake needs to run)
        if (menu is MonoBehaviour menuBehaviour && !menuBehaviour.gameObject.activeSelf)
        {
            menuBehaviour.gameObject.SetActive(true);
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
