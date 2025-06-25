using UnityEngine;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IInputService>().To<InputService>().AsSingle().NonLazy();
        Debug.Log("ProjectInstaller: IInputService bound to InputService as a singleton.");
    }
}