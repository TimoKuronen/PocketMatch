using System;
using UnityEngine;

public class MenuServices : Services
{
    protected override void InitializeSceneServices()
    {
        AddSceneService<IInputService>(new InputService());
        AddSceneService<ISaveService>(new SaveManager());

        InitializeAllSceneServices();
    }
}