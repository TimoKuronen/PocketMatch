using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandInvoker
{
    #region Fields

    private readonly Queue<ICommand> commandQueue = new();
    private MonoBehaviour runner;
    private Coroutine runningCoroutine;
    private bool isProcessingQueue;

    #endregion

    #region Constructor

    public CommandInvoker(MonoBehaviour runner)
    {
        this.runner = runner;
    }

    #endregion

    #region Public Methods

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

    #endregion

    #region Private Methods

    private IEnumerator RunQueue()
    {
        isProcessingQueue = true;

        while (commandQueue.Count > 0)
        {
            yield return commandQueue.Dequeue().Execute();
        }

        isProcessingQueue = false;
    }

    #endregion
}
