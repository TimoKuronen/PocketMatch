using UnityEngine;

public class InputService : IInputService
{
    private readonly GameControls controls;
    public Vector2 TouchPosition { get; private set; }
    public bool IsTouching { get; private set; }

    public InputService()
    {
        controls = new GameControls();
        controls.Gameplay.TouchPosition.performed += ctx => TouchPosition = ctx.ReadValue<Vector2>();
        controls.Gameplay.TouchPress.performed += _ => IsTouching = true;
        controls.Gameplay.TouchPress.canceled += _ => IsTouching = false;
        controls.Enable();

        Debug.Log("InputService initialized and controls enabled.");
    }

    public void Dispose() => controls.Dispose();
}
