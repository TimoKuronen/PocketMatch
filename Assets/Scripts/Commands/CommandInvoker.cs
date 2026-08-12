using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CommandInvoker
{
    private readonly Queue<ICommand> commandQueue = new();
    private bool isProcessingQueue;

    public void AddCommand(ICommand command)
    {
        commandQueue.Enqueue(command);
    }

    public void ExecuteAll()
    {
        if (!isProcessingQueue)
        {
            RunQueueAsync().Forget();
        }
    }

    public bool IsEmpty()
    {
        return commandQueue.Count == 0 && !isProcessingQueue;
    }

    private async UniTaskVoid RunQueueAsync()
    {
        isProcessingQueue = true;

        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            if (command != null)
            {
                await command.ExecuteAsync();
            }
        }

        isProcessingQueue = false;
    }
}
