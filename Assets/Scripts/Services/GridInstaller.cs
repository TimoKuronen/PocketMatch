using Zenject;

public class GridInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<GridService>()
            .AsSingle()
            .WithArguments(6, 8); // width, height
    }
}