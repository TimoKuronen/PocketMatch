using System;
using UnityEngine;

/// <summary>
/// Placeholder level select panel view for future expansion.
/// </summary>
public class LevelSelectPanel : UIMenu, ILevelSelectView
{
    public event Action<int> LevelSelected;
    public event Action BackClicked;
}

