using fundo.core;
using fundo.gui;
using fundo.gui.Job;
using fundo.gui.Job.Jobs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace fundo.tool;

/// <summary>
/// Manages scheduled in-process index updates.
/// Updates only run while the main window is hidden in the notification area.
/// </summary>
internal sealed class ScheduledIndexUpdateService : IDisposable
{
    private const int IdleThresholdMinutes = 5;

    private readonly DispatcherQueueTimer _timer;
    private readonly IntPtr _windowHandle;
    private bool _isIndexing;
    private DateTime _lastIndexRun = DateTime.MinValue;

    /// <summary>
    /// Returns <c>true</c> while a scheduled index update is in progress.
    /// </summary>
    public bool IsIndexing => _isIndexing;

    /// <summary>
    /// Raised on the UI thread whenever <see cref="IsIndexing"/> changes.
    /// </summary>
    public event Action? IndexingStateChanged;

    public ScheduledIndexUpdateService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(1);
        _timer.Tick += OnTimerTick;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_isIndexing)
            return;

        if (!Settings.AutomaticIndexUpdateEnabled)
            return;

        if (!Settings.UseIndex)
            return;

        if (IsWindowVisible(_windowHandle))
            return;

        if (!IsTimeToRun())
            return;

        _ = RunIndexUpdateAsync();
    }

    private bool IsTimeToRun()
    {
        DateTime now = DateTime.Now;
        TimeSpan interval = GetIntervalTimeSpan();

        if (now - _lastIndexRun < interval)
            return false;

        // For daily or longer intervals, only trigger near the preferred time of day
        if (interval >= TimeSpan.FromDays(1))
        {
            TimeSpan preferredTime = Settings.AutomaticIndexUpdatePreferredTime;
            TimeSpan diff = (now.TimeOfDay - preferredTime).Duration();
            return diff <= TimeSpan.FromMinutes(30);
        }

        return true;
    }

    private async Task RunIndexUpdateAsync()
    {
        _isIndexing = true;
        IndexingStateChanged?.Invoke();

        try
        {
            List<Drive> drives = await Task.Run(() => DriveUtil.GetDrives());

            DriveIndexingJob job = new(drives)
            {
                Priority = JobPriority.Low,
                BlocksUI = false
            };

            await JobScheduler.Instance.ScheduleAndWaitAsync(job);
            _lastIndexRun = DateTime.Now;
        }
        catch
        {
            // Don't let a scheduled update error crash the application
        }
        finally
        {
            _isIndexing = false;
            IndexingStateChanged?.Invoke();
            NotifyIconService.UpdateTooltip("Fundo");
        }
    }

    private static TimeSpan GetIntervalTimeSpan()
    {
        return Settings.AutomaticIndexUpdateInterval switch
        {
            ScheduledIndexUpdateInterval.Hourly => TimeSpan.FromHours(1),
            ScheduledIndexUpdateInterval.EverySixHours => TimeSpan.FromHours(6),
            ScheduledIndexUpdateInterval.Daily => TimeSpan.FromDays(1),
            ScheduledIndexUpdateInterval.EveryTwoDays => TimeSpan.FromDays(2),
            ScheduledIndexUpdateInterval.Weekly => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(1)
        };
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}
