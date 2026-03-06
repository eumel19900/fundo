using System;
using System.ComponentModel;

namespace fundo.gui.Job
{
    /// <summary>
    /// Holds the current status information of a running job.
    /// Implements INotifyPropertyChanged for UI binding.
    /// </summary>
    public class JobStatus : INotifyPropertyChanged
    {
        private double _progress;
        private double _maxProgress;
        private bool _isIndeterminate;
        private string _title;
        private string _description;
        private JobState _state;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Current progress value (0 to MaxProgress).
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        /// <summary>
        /// Maximum progress value. Default is 100.
        /// </summary>
        public double MaxProgress
        {
            get => _maxProgress;
            set
            {
                if (_maxProgress != value)
                {
                    _maxProgress = value;
                    OnPropertyChanged(nameof(MaxProgress));
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        /// <summary>
        /// Progress as percentage (0-100).
        /// </summary>
        public double ProgressPercentage =>
            _maxProgress > 0 ? (_progress / _maxProgress) * 100.0 : 0.0;

        /// <summary>
        /// If true, progress is indeterminate (unknown duration).
        /// </summary>
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                if (_isIndeterminate != value)
                {
                    _isIndeterminate = value;
                    OnPropertyChanged(nameof(IsIndeterminate));
                }
            }
        }

        /// <summary>
        /// Short title describing the current task.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Detailed description of the current operation.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// Current execution state of the job.
        /// </summary>
        public JobState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        /// <summary>
        /// True if the job is currently running.
        /// </summary>
        public bool IsRunning => _state == JobState.Running;

        /// <summary>
        /// True if the job has finished (completed, cancelled, or failed).
        /// </summary>
        public bool IsCompleted =>
            _state == JobState.Completed ||
            _state == JobState.Cancelled ||
            _state == JobState.Failed;

        public JobStatus()
        {
            _progress = 0;
            _maxProgress = 100;
            _isIndeterminate = false;
            _title = string.Empty;
            _description = string.Empty;
            _state = JobState.Pending;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
