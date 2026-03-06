using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace fundo.gui.Job
{
    /// <summary>
    /// A status bar control that displays the progress of background jobs.
    /// Place this at the bottom of your MainWindow.
    /// </summary>
    public sealed partial class JobStatusBar : UserControl
    {
        private JobBase? _currentJob;

        public JobStatusBar()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Subscribe to scheduler events
            JobScheduler scheduler = JobScheduler.Instance;
            scheduler.JobStarted += OnJobStarted;
            scheduler.JobCompleted += OnJobCompleted;
            scheduler.JobStatusChanged += OnJobStatusChanged;

            // Check if there's already a running job
            if (scheduler.CurrentJob != null && !scheduler.CurrentJob.BlocksUI)
            {
                AttachToJob(scheduler.CurrentJob);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from scheduler events
            JobScheduler scheduler = JobScheduler.Instance;
            scheduler.JobStarted -= OnJobStarted;
            scheduler.JobCompleted -= OnJobCompleted;
            scheduler.JobStatusChanged -= OnJobStatusChanged;

            DetachFromJob();
        }

        private void OnJobStarted(object? sender, JobBase job)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Only show status bar for non-blocking jobs
                if (!job.BlocksUI)
                {
                    AttachToJob(job);
                }
            });
        }

        private void OnJobCompleted(object? sender, JobBase job)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_currentJob?.Id == job.Id)
                {
                    DetachFromJob();
                }
            });
        }

        private void OnJobStatusChanged(object? sender, JobBase job)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_currentJob?.Id == job.Id)
                {
                    UpdateStatusDisplay(job.Status);
                }
            });
        }

        private void AttachToJob(JobBase job)
        {
            _currentJob = job;
            RootGrid.Visibility = Visibility.Visible;
            UpdateStatusDisplay(job.Status);
        }

        private void DetachFromJob()
        {
            _currentJob = null;
            RootGrid.Visibility = Visibility.Collapsed;
            StatusProgressRing.IsActive = false;
        }

        private void UpdateStatusDisplay(JobStatus status)
        {
            TitleTextBlock.Text = status.Title ?? "Working...";
            DescriptionTextBlock.Text = status.Description ?? string.Empty;

            if (status.IsIndeterminate)
            {
                StatusProgressBar.IsIndeterminate = true;
                StatusProgressRing.IsActive = true;
            }
            else
            {
                StatusProgressBar.IsIndeterminate = false;
                StatusProgressBar.Maximum = status.MaxProgress;
                StatusProgressBar.Value = status.Progress;
                StatusProgressRing.IsActive = true;
            }

            // Update cancel button state
            CancelButton.IsEnabled = status.State == JobState.Running;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _currentJob?.Cancel();
            CancelButton.IsEnabled = false;
            TitleTextBlock.Text = "Cancelling...";
        }
    }
}
