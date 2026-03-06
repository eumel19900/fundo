using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace fundo.gui.Job
{
    /// <summary>
    /// A modal ContentDialog that displays job progress and allows cancellation.
    /// Used when JobBase.BlocksUI is true.
    /// </summary>
    public sealed class JobProgressDialog : ContentDialog
    {
        private readonly JobBase _job;
        private readonly ProgressBar _progressBar;
        private readonly ProgressRing _progressRing;
        private readonly TextBlock _titleText;
        private readonly TextBlock _descriptionText;
        private bool _isClosing;

        public JobProgressDialog(JobBase job)
        {
            _job = job ?? throw new ArgumentNullException(nameof(job));

            Title = job.JobName;
            PrimaryButtonText = "Cancel";
            IsPrimaryButtonEnabled = true;

            // Create UI elements
            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                IsIndeterminate = job.Status.IsIndeterminate,
                Width = 400,
                Margin = new Thickness(0, 10, 0, 10)
            };

            _progressRing = new ProgressRing
            {
                IsActive = job.Status.IsIndeterminate,
                Width = 40,
                Height = 40,
                Visibility = job.Status.IsIndeterminate ? Visibility.Visible : Visibility.Collapsed
            };

            _titleText = new TextBlock
            {
                Text = job.Status.Title ?? job.JobName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };

            _descriptionText = new TextBlock
            {
                Text = job.Status.Description ?? string.Empty,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                MaxWidth = 400
            };

            StackPanel contentPanel = new()
            {
                Spacing = 8,
                Width = 420,
                Children =
                {
                    _titleText,
                    _descriptionText,
                    _progressBar,
                    _progressRing
                }
            };

            Content = contentPanel;

            // Wire up events
            PrimaryButtonClick += OnCancelClicked;
            _job.StatusChanged += OnJobStatusChanged;
            _job.Completed += OnJobCompleted;
        }

        private void OnCancelClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Defer closing to allow cancellation to process
            args.Cancel = true;
            _job.Cancel();
            IsPrimaryButtonEnabled = false;
            _titleText.Text = "Cancelling...";
        }

        private void OnJobStatusChanged(object? sender, JobStatus status)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_isClosing) return;

                _titleText.Text = status.Title ?? _job.JobName;
                _descriptionText.Text = status.Description ?? string.Empty;

                if (status.IsIndeterminate)
                {
                    _progressBar.IsIndeterminate = true;
                    _progressRing.IsActive = true;
                    _progressRing.Visibility = Visibility.Visible;
                }
                else
                {
                    _progressBar.IsIndeterminate = false;
                    _progressBar.Maximum = status.MaxProgress;
                    _progressBar.Value = status.Progress;
                    _progressRing.IsActive = false;
                    _progressRing.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isClosing = true;
                _job.StatusChanged -= OnJobStatusChanged;
                _job.Completed -= OnJobCompleted;
                Hide();
            });
        }
    }
}
