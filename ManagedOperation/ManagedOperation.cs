/*
 * ---------------------------------------- Managed Operations ----------------------------------------------|
 * by: MarcWerk. October 2023                                                                                |
 * License: MIT                                                                                              |
 *                                                                                                           |
 * Managed Operations is a collection of classes to manage different types of asynchronous operations.       |
 * It allows you to chain together operations like animations and delays,                                    |
 * by giving you access to controls like starting, completing and cancelling that can be assigned callbacks. |
 * The different types of Operation class is a powerful tool if used in conjunction with one and other.      |
 * For example: having a ManagedReturnValueOperation managed by a CoroutineOperation.                        |
 *                                                                                                           |
 * Disclaimer:                                                                                               |
 * This script uses a modified version of the MEC (More Efficient Coroutines) asset.                         |
 * The modified asset should be packaged alongside this asset.                                               |
 * If not contact MarcWerk at the GodotAssetRepo YouTube channel.                                            |
 * ----------------------------------------------------------------------------------------------------------|
 */

using Godot;
using MEC;
using System.Collections.Generic;

/// <summary>
/// The abstract class that serves as a base for all other Managed Operations
/// </summary>
public abstract class Operation
{
    public delegate void OnCancelledHandler(OperationError error);

    protected OnCancelledHandler OnCancelled;
    protected Timing timing => Timing.Instance;

    public bool IsWaiting => State == OperationState.Waiting;
    public bool IsRunning => State == OperationState.Running;
    public bool IsCancelled => State == OperationState.Cancelled;
    public bool IsCompleted => State == OperationState.Completed;
    public bool IsEnded => State == OperationState.Cancelled || State == OperationState.Completed;

    public OperationError Error { get; protected set; } = null;

    public OperationState State { get; protected set; } = OperationState.Waiting;

    protected abstract void OnCancel(OperationError error = null);

    /// <summary>
    /// Starts the current operation, unless 'startOnCreate' is specifically set to false during construction. This is not needed.
    /// </summary>
    public virtual Operation Run ()
    {
        if(State == OperationState.Waiting)
        {
            State = OperationState.Running;
        }
        return this;
    }

    /// <summary>
    /// Cancels the currently running operation
    /// </summary>
    /// <param name="error">An optional error object mainly for debugging</param>
    public void Cancel(OperationError error = null)
    {
        OnCancel();
    }

    /// <summary>
    /// Register a callback in the event the Operation is cancelled, if the operation is already cancelled at this time the callback is invoked immediately
    /// </summary>
    /// <param name="cancelledHandler">Callback</param>
    public virtual Operation ContinueWithOnCancel(OnCancelledHandler cancelledHandler)
    {
        if (State != OperationState.Cancelled)
            OnCancelled += cancelledHandler;
        else cancelledHandler?.Invoke(Error);

        return this;
    }

    public enum OperationState
    {
        Waiting, Running, Cancelled, Completed
    }
}
/// <summary>
/// The Managed Operation class is the most basic of the Operation classes. It can be manually started and completed/cancelled.
/// Making it ideal for grouping multiple operations together in a single callback.
/// </summary>
public class ManagedOperation : Operation
{
    public delegate void OnCompletedHandler();
    public delegate void OnEndHandler(bool success, OperationError error);

    protected OnCompletedHandler OnCompleted;
    protected OnEndHandler OnEnd;

    public ManagedOperation (bool runOnCreate = true)
    {
        if (runOnCreate)
            Run();
    }

    public override ManagedOperation Run()
    {
        return base.Run() as ManagedOperation;
    }

    /// <summary>
    /// Completes the currently running operation
    /// </summary>
    public void Complete()
    {
        OnComplete();
    }

    void OnComplete()
    {
        if (State <= OperationState.Running)
        {
            State = OperationState.Completed;
            OnCompleted?.Invoke();
            OnEnd?.Invoke(true, Error);
        }
    }

    /// <summary>
    /// Register a callback in the event the Operation is completed, if the operation is already completed at this time the callback is invoked immediately
    /// </summary>
    /// <param name="completedHandler">Callback</param>
    public virtual ManagedOperation ContinueWithOnComplete(OnCompletedHandler completedHandler)
    {
        if (State != OperationState.Completed)
            OnCompleted += completedHandler;
        else completedHandler?.Invoke();
        return this;
    }

    public override ManagedOperation ContinueWithOnCancel(OnCancelledHandler cancelledHandler)
    {
        return base.ContinueWithOnCancel(cancelledHandler) as ManagedOperation;
    }

    /// <summary>
    /// Register a callback for when the operation is ended, if the operation is already ended at this time the callback is invoked immediately
    /// </summary>
    /// <param name="endHandler">Callback</param>
    public virtual ManagedOperation ContinueWithOnEnd (OnEndHandler endHandler)
    {
        if (State <= OperationState.Running)
            OnEnd += endHandler;
        else OnEnd?.Invoke(State == OperationState.Cancelled ? false : true, Error);
        return this;
    }

    protected override void OnCancel(OperationError error = null)
    {
        if (State != OperationState.Cancelled)
        {
            Error = error;

            if (error != null)
                GD.PrintErr(Error.ToString());

            State = OperationState.Cancelled;
            OnCancelled?.Invoke(Error);
            OnEnd?.Invoke(false, Error);
        }
    }
}
/// <summary>
/// The CoroutineOperation allows you to manage running coroutines. Starting and Cancelling them. 
/// Callbacks can be assigned to both completion and cancellation allowing you to chain together multiple operations.
/// </summary>
public class CoroutineOperation : ManagedOperation
{

    CoroutineHandle routine;
    CoroutineHandle wrapperRoutine;

    IEnumerator<double> ienumerator;

    public CoroutineOperation (IEnumerator<double> ienumerator, bool runOnCreate = true) : base (false)
    {
        this.ienumerator = ienumerator;
        if(ienumerator == null)
        {
            State = OperationState.Completed;
        } 
        else if (runOnCreate)
            Run();
    }

    IEnumerator<double> Wrapper (CoroutineHandle coroutine)
    {
        while (Timing.IsRunning(coroutine))
            yield return Timing.WaitForOneFrame;
            //yield return Timing.WaitUntilDone(routine);
        Complete();
    }

    public override CoroutineOperation Run()
    {
        if ( State == OperationState.Waiting)
        {
            if (ienumerator == null)
            {
                Complete();
            }
            else
            {
                State = OperationState.Running;
                routine = Timing.RunCoroutine(ienumerator);
                wrapperRoutine = Timing.RunCoroutine(Wrapper(routine));
            }
        }
        return this;
    }

    protected override void OnCancel(OperationError error = null)
    {
        if (State == OperationState.Running)
        {
            timing.KillCoroutinesOnInstance(wrapperRoutine);
            timing.KillCoroutinesOnInstance(routine);
        }
        base.OnCancel(error);
    }

    public override CoroutineOperation ContinueWithOnComplete(OnCompletedHandler completedHandler)
    {
        return base.ContinueWithOnComplete(completedHandler) as CoroutineOperation;
    }

    public override CoroutineOperation ContinueWithOnCancel(OnCancelledHandler cancelledHandler)
    {
        return base.ContinueWithOnCancel(cancelledHandler) as CoroutineOperation;
    }

    public override CoroutineOperation ContinueWithOnEnd(OnEndHandler endHandler)
    {
        return base.ContinueWithOnEnd(endHandler) as CoroutineOperation;
    }

    /// <summary>
    /// A non-running, already completed, operation. Usefull if a operation is expected as a return value but not needed.
    /// </summary>
    public static CoroutineOperation NullOperation => new CoroutineOperation(null);
}

/// <summary>
/// The ManagedReturnValueOperation class is useful for managing asynchronous tasks like file loading or waiting for user input.
/// The class can be manually completed with a Result value.
/// </summary>
/// <typeparam name="T">Return value type</typeparam>
public class ManagedReturnOperation<T> : Operation
{
    public delegate void OnCompletedHandler(T result);
    public delegate void OnEndHandler(bool success, OperationError error, T result);

    protected OnCompletedHandler OnCompleted;
    protected OnEndHandler OnEnd;

    public T Result { get; private set; }

    protected override void OnCancel(OperationError error = null)
    {
        if(State == OperationState.Running)
        {
            Error = error;

            if(error != null)
                GD.PrintErr(error.ToString());

            State = OperationState.Cancelled;
            OnCancelled?.Invoke(Error);
            OnEnd?.Invoke(false, Error, default(T));
        }
    }

    public override ManagedReturnOperation<T> Run()
    {
        return base.Run() as ManagedReturnOperation<T>;
    }

    /// <summary>
    /// Completes the currently running operation with a given result
    /// </summary>
    /// <param name="result">result as T</param>
    public void Complete (T result)
    {
        if (State == OperationState.Running)
        {
            Result = result;
            State = OperationState.Completed;
            OnCompleted?.Invoke(result);
            OnEnd.Invoke(true, Error, Result);
        }
    }


    /// <summary>
    /// Register a callback in the event the Operation is completed, if the operation is already completed at this time the callback is invoked immediately
    /// </summary>
    /// <param name="completedHandler">Callback</param>
    public ManagedReturnOperation<T> ContinueWithOnComplete(OnCompletedHandler completedHandler)
    {
        if (State != OperationState.Completed)
            OnCompleted += completedHandler;
        else completedHandler?.Invoke(Result);
        return this;
    }


    /// <summary>
    /// Register a callback for when the operation is ended, if the operation is already ended at this time the callback is invoked immediately
    /// </summary>
    /// <param name="endHandler">Callback</param>
    public ManagedReturnOperation<T> ContinueWithOnEnd(OnEndHandler endHandler)
    {
        if (State <= OperationState.Running)
            OnEnd += endHandler;
        else OnEnd?.Invoke(State == OperationState.Cancelled ? false : true, Error, Result);
        return this;
    }

    /// <summary>
    /// Register a callback in the event the Operation is cancelled, if the operation is already cancelled at this time the callback is invoked immediately
    /// </summary>
    /// <param name="cancelledHandler">Callback</param>
    public override ManagedReturnOperation<T> ContinueWithOnCancel(OnCancelledHandler cancelledHandler)
    {
        return base.ContinueWithOnCancel(cancelledHandler) as ManagedReturnOperation<T>;
    }
}

public class OperationError
{
    public string Message { get; private set; }
    public ErrorSeverity severity { get; private set; } = ErrorSeverity.Warning;

    public OperationError (string message, ErrorSeverity severity)
    {
        Message = message;
        this.severity = severity;
    }

    public enum ErrorSeverity
    {
        Warning, Error
    }

    public override string ToString()
    {
        return $"[{severity}] Operation Error: {Message}";
    }
}