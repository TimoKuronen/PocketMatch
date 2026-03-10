using Cysharp.Threading.Tasks;

public interface ICommand
{
    UniTask ExecuteAsync();
}