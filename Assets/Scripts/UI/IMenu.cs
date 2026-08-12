using System;

/// <summary>
/// Stack-managed overlay menu driven by <see cref="MenuStackManager"/>.
/// </summary>
public interface IMenu
{
    MenuType MenuType { get; }
    bool IsOpen { get; }

    void Open();
    void Close();
    bool CanOpen();

    event Action OnMenuOpened;
    event Action OnMenuClosed;
}
