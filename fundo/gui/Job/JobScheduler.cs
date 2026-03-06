using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace fundo.gui.Job
{
    /// <summary>
    /// Singleton scheduler that manages and executes jobs.
    /// Jobs are executed based on priority, one at a time.
    /// </summary>
    public sealed class JobScheduler
    {
        private static JobScheduler? _instance;
        private static readonly object _lock = new();

        private readonly PriorityQueue<JobBase, int> _pendingJobs = new();
        private readonly List<JobBase> _runningJobs = new();
        private readonly List<JobBase> _completedJobs = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly DispatcherQueue _dispatcherQueue;

        private XamlRoot? _xamlRoot;
        private bool _isProcessing;

        /// <summary>
        /// Gets the singleton instance of the JobScheduler.
        /// Must be called from the UI thread on first access.
        /// </summary>
        public static JobScheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new JobScheduler();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when a job is added to the queue.
        /// </summary>
        public event EventHandler<JobBase>? JobQueued;

        /// <summary>
        /// Event raised when a job starts executing.
        /// </summary>
        public event EventHandler<JobBase>? JobStarted;

        /// <summary>
        /// Event raised when a job completes (success, cancel, or failure).
        /// </summary>
        public event EventHandler<JobBase>? JobCompleted;

        /// <summary>
        /// Event raised when the status of any running job changes.
        /// </summary>
        public event EventHandler<JobBase>? JobStatusChanged;

        /// <summary>
        /// Returns the currently running job, or null if none.
        /// </summary>
        public JobBase? CurrentJob => _runningJobs.FirstOrDefault();

        /// <summary>
        /// Returns true if any job is currently running.
        /// </summary>
        public bool HasRunningJobs => _runningJobs.Count > 0;

        /// <summary>
        /// Returns the number of pending jobs in the queue.
        /// </summary>
        public int PendingJobCount => _pendingJobs.Count;

        private JobScheduler()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException("JobScheduler must be initialized on a UI thread.");
        }

        /// <summary>
        /// Initializes the scheduler with the XamlRoot for displaying dialogs.
        /// Call this once from MainWindow after loading.
        /// </summary>
        public void Initialize(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;
        }

        /// <summary>
        /// Schedules a job for execution.
        /// </summary>
        public void Schedule(JobBase job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            // Priority is inverted because PriorityQueue uses lower = higher priority
            int priority = -(int)job.Priority;

            lock (_lock)
            {
                _pendingJobs.Enqueue(job, priority);
            }

            JobQueued?.Invoke(this, job);
            ProcessQueueAsync();
        }

        /// <summary>
        /// Schedules a job and waits for its completion.
        /// </summary>
        public async Task ScheduleAndWaitAsync(JobBase job)
        {
            TaskCompletionSource<bool> tcs = new();

            job.Completed += (s, e) => tcs.TrySetResult(true);
            Schedule(job);

            await tcs.Task;
        }

        /// <summary>
        /// Cancels a specific job by its ID.
        /// </summary>
        public void Cancel(Guid jobId)
        {
            JobBase? job = _runningJobs.FirstOrDefault(j => j.Id == jobId);
            job?.Cancel();
        }

        /// <summary>
        /// Cancels all running and pending jobs.
        /// </summary>
        public void CancelAll()
        {
            lock (_lock)
            {
                while (_pendingJobs.Count > 0)
                {
                    _pendingJobs.Dequeue();
                }
            }

            foreach (JobBase job in _runningJobs.ToList())
            {
                job.Cancel();
            }
        }

        private async void ProcessQueueAsync()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            try
            {
                while (true)
                {
                    JobBase? job = null;

                    lock (_lock)
                    {
                        if (_pendingJobs.Count == 0)
                            break;

                        job = _pendingJobs.Dequeue();
                    }

                    if (job != null)
                    {
                        await ExecuteJobAsync(job);
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task ExecuteJobAsync(JobBase job)
        {
            _runningJobs.Add(job);

            job.StatusChanged += OnJobStatusChanged;
            job.Completed += OnJobCompleted;

            JobStarted?.Invoke(this, job);

            if (job.BlocksUI && _xamlRoot != null)
            {
                // Show modal progress dialog
                await ShowProgressDialogAsync(job);
            }
            else
            {
                // Run in background, status bar will show progress
                await job.RunAsync();
            }

            job.StatusChanged -= OnJobStatusChanged;
            job.Completed -= OnJobCompleted;

            _runningJobs.Remove(job);
            _completedJobs.Add(job);
        }

        private async Task ShowProgressDialogAsync(JobBase job)
        {
            JobProgressDialog dialog = new(job)
            {
                XamlRoot = _xamlRoot!
            };

            // Start job execution
            Task jobTask = job.RunAsync();

            // Show dialog (it will close when job completes)
            await dialog.ShowAsync();

            // Ensure job task completes
            await jobTask;
        }

        private void OnJobStatusChanged(object? sender, JobStatus status)
        {
            if (sender is JobBase job)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    JobStatusChanged?.Invoke(this, job);
                });
            }
        }

        private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
        {
            if (sender is JobBase job)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    JobCompleted?.Invoke(this, job);
                });
            }
        }
    }
}
