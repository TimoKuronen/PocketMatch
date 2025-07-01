using UnityEngine;

public interface IInputService : IService
{
    Vector2 TouchPosition { get; }
    bool IsTouching { get; }
}