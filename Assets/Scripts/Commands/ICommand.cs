using System.Collections;

public interface ICommand
{
    IEnumerator Execute();
}