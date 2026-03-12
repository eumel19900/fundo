using System;
using System.Threading;

namespace fundo.core;

internal sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Global\\Fundo_SingleInstance_Mutex";
    private const string EventName = "Global\\Fundo_SingleInstance_BringToFront";

    private Mutex? _mutex;
    private EventWaitHandle? _bringToFrontEvent;
    private Thread? _listenerThread;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Tries to claim the single-instance lock.
    /// Returns <c>true</c> if this is the first instance.
    /// Returns <c>false</c> if another instance is already running
    /// (in that case the other instance has been signalled to come to the foreground).
    /// </summary>
    public bool TryClaimInstance()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (createdNew)
        {
            return true;
        }

        // Another instance owns the mutex – signal it and let this instance exit.
        try
        {
            using var existingEvent = EventWaitHandle.OpenExisting(EventName);
            existingEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance has not created the event yet – nothing we can do.
        }

        return false;
    }

    /// <summary>
    /// Starts listening for activation requests from subsequent instances.
    /// <paramref name="bringToFrontCallback"/> is invoked on the calling thread's
    /// <see cref="SynchronizationContext"/> when another instance signals this one.
    /// </summary>
    public void StartListening(Action bringToFrontCallback)
    {
        _bringToFrontEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        _cts = new CancellationTokenSource();

        SynchronizationContext? uiContext = SynchronizationContext.Current;
        CancellationToken token = _cts.Token;

        _listenerThread = new Thread(() => ListenForActivation(uiContext, bringToFrontCallback, token))
        {
            IsBackground = true,
            Name = "SingleInstanceListener"
        };
        _listenerThread.Start();
    }

    private void ListenForActivation(SynchronizationContext? uiContext, Action callback, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Wait with a timeout so we can periodically check for cancellation.
                bool signalled = _bringToFrontEvent!.WaitOne(TimeSpan.FromMilliseconds(500));
                if (signalled && !token.IsCancellationRequested)
                {
                    if (uiContext != null)
                    {
                        uiContext.Post(_ => callback(), null);
                    }
                    else
                    {
                        callback();
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Event handle was disposed during shutdown – expected.
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();

        _bringToFrontEvent?.Dispose();
        _bringToFrontEvent = null;

        try { _listenerThread?.Join(TimeSpan.FromSeconds(2)); } catch { }
        _listenerThread = null;

        _cts?.Dispose();
        _cts = null;

        if (_mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
