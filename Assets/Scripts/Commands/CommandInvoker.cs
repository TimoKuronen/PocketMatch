using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandInvoker
{
    private readonly Queue<ICommand> commandQueue = new();
    private MonoBehaviour runner;

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
        runner.StartCoroutine(RunQueue());
    }

    public bool IsEmpty() => commandQueue.Count == 0;

    private IEnumerator RunQueue()
    {
        while (commandQueue.Count > 0)
        {
            //Debug.Log($"Executing command: {commandQueue.Peek().GetType().Name}");
            yield return commandQueue.Dequeue().Execute();
        }
    }
}
