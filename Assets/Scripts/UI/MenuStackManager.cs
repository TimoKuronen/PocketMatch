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
    /// Removes the target menu from the stack, closing every menu above it and reopening menus below.
    /// </summary>
    public bool PopMenuOfType(MenuType menuType)
    {
        if (!HasMenuOfType(menuType))
            return false;

        var menusToRestore = new Stack<IMenu>();
        bool found = false;

        while (menuStack.Count > 0)
        {
            var menu = menuStack.Pop();
            menu.Close();

            if (menu.MenuType == menuType)
            {
                found = true;
                break; // Do not restore menus that were above the target.
            }

            menusToRestore.Push(menu);
        }

        // Menus below the target were closed during the pop walk; put them back on the stack opened.
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
    /// Supplies the play-scene grid so <see cref="CanOpenMenu"/> can block UI during tile processing.
    /// </summary>
    public void SetGridController(IGridController gridController)
    {
        this.gridController = gridController;
    }

    /// <summary>
    /// Returns false while the board is resolving matches; main-menu scenes allow menus when no grid is bound.
    /// </summary>
    public bool CanOpenMenu()
    {
        if (gridController == null)
            return true;

        return !gridController.IsProcessingTiles;
    }

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

        // Inactive menu objects must be enabled so Awake runs before Open.
        if (menu is MonoBehaviour menuBehaviour && !menuBehaviour.gameObject.activeSelf)
        {
            menuBehaviour.gameObject.SetActive(true);
        }

        menuStack.Push(menu);
        menu.Open();
        return true;
    }

    public bool PopMenu()
    {
        if (menuStack.Count == 0)
        {
            Debug.LogWarning("[MenuStackManager] Attempted to pop from empty stack");
            return false;
        }

        var menu = menuStack.Pop();
        menu.Close();
        return true;
    }

    public void PopAllMenus()
    {
        while (menuStack.Count > 0)
        {
            PopMenu();
        }
    }

    /// <summary>
    /// Closes every menu and clears the stack without per-pop warnings; used when leaving a level.
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
