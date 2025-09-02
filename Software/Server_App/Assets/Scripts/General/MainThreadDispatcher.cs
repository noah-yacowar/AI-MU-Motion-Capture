using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private ConcurrentQueue<Action> tasks;

    // Might need to put this in awake... must check
    void Start()
    {
        Instance = this; 
        tasks = new ConcurrentQueue<Action>();
    }

    public void DispatchAction(Action newAction)
    {
        tasks.Enqueue(newAction);
    }

    void Update()
    {
        //Triggering actions dispatched by threads
        while (tasks.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }
}
