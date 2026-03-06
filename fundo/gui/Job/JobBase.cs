using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace fundo.gui.Job
{
    /// <summary>
    /// Abstract base class for long-running jobs.
    /// Derive from this class and implement Execute() to create custom jobs.
    /// No async/await required - the framework handles background execution.
    /// </summary>
    public abstract class JobBase
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly DispatcherQueue _dispatcherQueue;

        /// <summary>
        /// Unique identifier for this job instance.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Priority of this job in the scheduler queue.
        /// </summary>
        public JobPriority Priority { get; set; } = JobPriority.Normal;

        /// <summary>
        /// If true, a modal progress dialog will block the UI during execution.
        /// If false, progress is shown in the status bar.
        /// </summary>
        public bool BlocksUI { get; set; } = false;

        /// <summary>
        /// Current status information of this job.
        /// </summary>
        public JobStatus Status { get; } = new JobStatus();

        /// <summary>
        /// Name of the job (for display purposes).
        /// </summary>
        public abstract string JobName { get; }

        /// <summary>
        /// Exception that occurred during execution, if any.
        /// </summary>
        public Exception? Error { get; private set; }

        /// <summary>
        /// The cancellation token for checking cancellation requests within Execute().
        /// </summary>
        protected CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Event raised when the job completes (success, cancel, or failure).
        /// </summary>
        public event EventHandler<JobCompletedEventArgs>? Completed;

        /// <summary>
        /// Event raised when the job status changes.
        /// </summary>
        public event EventHandler<JobStatus>? StatusChanged;

        protected JobBase()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException("JobBase must be created on a UI thread.");
        }

        /// <summary>
        /// Starts the job execution asynchronously.
        /// The job runs on a background thread - no async/await needed in Execute().
        /// </summary>
        public async Task RunAsync()
        {
            if (Status.State == JobState.Running)
            {
                throw new InvalidOperationException("Job is already running.");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            Status.State = JobState.Running;
            Status.Title = JobName;
            RaiseStatusChanged();

            try
            {
                await Task.Run(() =>
                {
                    Execute();
                }, _cancellationTokenSource.Token);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Status.State = JobState.Cancelled;
                }
                else
                {
                    Status.State = JobState.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                Status.State = JobState.Cancelled;
            }
            catch (Exception ex)
            {
                Error = ex;
                Status.State = JobState.Failed;
                Status.Description = $"Error: {ex.Message}";
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                RaiseStatusChanged();
                RaiseCompleted();
            }
        }

        /// <summary>
        /// Requests cancellation of the job.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Override this method to implement the actual job logic.
        /// This runs on a background thread - no async/await needed.
        /// Use CancellationToken property or ThrowIfCancellationRequested() to handle cancellation.
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// Throws OperationCanceledException if cancellation has been requested.
        /// Call this periodically in your Execute() implementation.
        /// </summary>
        protected void ThrowIfCancellationRequested()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Returns true if cancellation has been requested.
        /// </summary>
        protected bool IsCancellationRequested => CancellationToken.IsCancellationRequested;

        /// <summary>
        /// Pauses execution for the specified duration.
        /// Respects cancellation requests.
        /// </summary>
        protected void Sleep(int milliseconds)
        {
            CancellationToken.WaitHandle.WaitOne(milliseconds);
            ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Pauses execution for the specified duration.
        /// Respects cancellation requests.
        /// </summary>
        protected void Sleep(TimeSpan duration)
        {
            CancellationToken.WaitHandle.WaitOne(duration);
            ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Updates the progress value. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void ReportProgress(int progress)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.Progress = progress;
                RaiseStatusChanged();
            });
        }

        /// <summary>
        /// Updates the progress with max value. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void ReportProgress(int progress, int maxProgress)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.MaxProgress = maxProgress;
                Status.Progress = progress;
                RaiseStatusChanged();
            });
        }

        /// <summary>
        /// Updates the task title. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void ReportTitle(string title)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.Title = title;
                RaiseStatusChanged();
            });
        }

        /// <summary>
        /// Updates the detailed description. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void ReportDescription(string description)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.Description = description;
                RaiseStatusChanged();
            });
        }

        /// <summary>
        /// Updates title and description at once. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void ReportStatus(string title, string description)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.Title = title;
                Status.Description = description;
                RaiseStatusChanged();
            });
        }

        /// <summary>
        /// Updates progress, title and description at once. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void ReportStatus(int progress, string title, string description)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.Progress = progress;
                Status.Title = title;
                Status.Description = description;
                RaiseStatusChanged();
            });
        }

        /// <summary>
        /// Sets progress to indeterminate mode. Call from within Execute().
        /// Thread-safe: dispatches to UI thread.
        /// </summary>
        protected void SetIndeterminate(bool isIndeterminate)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Status.IsIndeterminate = isIndeterminate;
                RaiseStatusChanged();
            });
        }

        private void RaiseStatusChanged()
        {
            StatusChanged?.Invoke(this, Status);
        }

        private void RaiseCompleted()
        {
            Completed?.Invoke(this, new JobCompletedEventArgs(Status.State, Error));
        }
    }

    /// <summary>
    /// Event arguments for job completion.
    /// </summary>
    public class JobCompletedEventArgs : EventArgs
    {
        public JobState FinalState { get; }
        public Exception? Error { get; }

        public JobCompletedEventArgs(JobState state, Exception? error)
        {
            FinalState = state;
            Error = error;
        }
    }
}
