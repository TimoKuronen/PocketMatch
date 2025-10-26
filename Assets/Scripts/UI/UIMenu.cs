using System;
using UnityEngine;

public abstract class UIMenu : MonoBehaviour
{
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;
    public event Action OnButtonPressed;
}
