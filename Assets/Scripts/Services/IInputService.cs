using UnityEngine;

public interface IInputService
{
    Vector2 TouchPosition { get; }
    bool IsTouching { get; }
}