using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

public class InputService : IInputService, ITickable
{
    private GameControls controls;
    
    public Vector2 TouchPosition { get; private set; }
    public bool IsTouching { get; private set; }

    [Inject]
    public void Construct()
    {
        controls = new GameControls();

        controls.Gameplay.TouchPosition.performed += ctx => TouchPosition = ctx.ReadValue<Vector2>();
        controls.Gameplay.TouchPress.performed += _ => IsTouching = true;
        controls.Gameplay.TouchPress.canceled += _ => IsTouching = false;

        controls.Enable();
    }

    public void Dispose()
    {
        controls.Disable();
        controls.Dispose();
    }

    public void Tick()
    {

    }
}
