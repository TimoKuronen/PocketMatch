using System;
using UnityEngine;

public abstract class UIMenu : MonoBehaviour
{
    public event Action MenuOpened;
    public event Action MenuClosed;
    public event Action ButtonPressed;
    public event Action OnButtonPressed;
}
