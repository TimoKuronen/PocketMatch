using System;
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

    /// <summary>
    /// A failing command drops the rest of the queue so waiters on <see cref="IsEmpty"/> are released
    /// instead of waiting on commands that will never run.
    /// </summary>
    private async UniTaskVoid RunQueueAsync()
    {
        isProcessingQueue = true;

        try
        {
            while (commandQueue.Count > 0)
            {
                var command = commandQueue.Dequeue();
                if (command != null)
                {
                    await command.ExecuteAsync();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            commandQueue.Clear();
        }
        finally
        {
            isProcessingQueue = false;
        }
    }
}
