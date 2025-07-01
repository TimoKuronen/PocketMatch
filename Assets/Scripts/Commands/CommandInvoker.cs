using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandInvoker
{
    private Stack<ICommand> history = new();

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        history.Push(command);
    }

    public void UndoLast()
    {
        if (history.Count > 0)
            history.Pop().Undo();
    }
}
