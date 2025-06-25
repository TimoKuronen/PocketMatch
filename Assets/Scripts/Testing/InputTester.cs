using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class InputTester : MonoBehaviour
{
    private IInputService input;

    [Inject]
    public void Construct(IInputService inputService)
    {
        input = inputService;
        Debug.Log("InputTester constructed with IInputService.");
    }

    private void Update()
    {
        if (input == null)
        {
            return;
        }

        if (input.IsTouching)
        {
           // Debug.Log($"Touching at: {input.TouchPosition}");
        }
    }
}
