using UnityEngine;

public class InputService : IInputService
{
    private GameControls controls;
    public Vector2 TouchPosition { get; private set; }
    public bool IsTouching { get; private set; }

    public void Initialize()
    {
        controls = new GameControls();
        controls.Gameplay.TouchPosition.performed += ctx => TouchPosition = ctx.ReadValue<Vector2>();
        controls.Gameplay.TouchPress.performed += _ => IsTouching = true;
        controls.Gameplay.TouchPress.canceled += _ => IsTouching = false;
        controls.Enable();
    }

    public void Dispose() => controls.Dispose();
}
