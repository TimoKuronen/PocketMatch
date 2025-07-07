using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandInvoker
{
    private readonly Queue<ICommand> commandQueue = new();
    private MonoBehaviour runner;
    private Coroutine runningCoroutine;
    private bool isProcessingQueue;

    public CommandInvoker(MonoBehaviour runner)
    {
        this.runner = runner;
    }

    public void AddCommand(ICommand command)
    {
        commandQueue.Enqueue(command);
    }

    public void ExecuteAll()
    {
        if (!isProcessingQueue)
        {
            runningCoroutine = runner.StartCoroutine(RunQueue());
        }
    }

    public bool IsEmpty()
    {
        return commandQueue.Count == 0 && !isProcessingQueue;
    }

    private IEnumerator RunQueue()
    {
        isProcessingQueue = true;

        while (commandQueue.Count > 0)
        {
            //Debug.Log($"Executing command: {commandQueue.Peek().GetType().Name}");
            yield return commandQueue.Dequeue().Execute();
        }

        isProcessingQueue = false;
    }
}
