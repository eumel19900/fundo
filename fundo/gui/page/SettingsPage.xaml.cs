using fundo.core;
using fundo.core.Persistence;
using fundo.gui.Job;
using fundo.gui.Job.Jobs;
using fundo.tool;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace fundo.gui.page
{
    /// <summary>
    /// Settings page for configuring indexed search.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private List<Drive> drives;
        private bool settingsSaved = false;
        private bool _isRecordingHotkey;

        public ScheduledIndexUpdateInterval[] ScheduledIndexUpdateIntervals { get; } = Enum.GetValues<ScheduledIndexUpdateInterval>();

        public bool IsScheduledIndexUpdateEnabled { get; set; } = Settings.AutomaticIndexUpdateEnabled;

        public ScheduledIndexUpdateInterval SelectedScheduledIndexUpdateInterval { get; set; } = Settings.AutomaticIndexUpdateInterval;

        public TimeSpan ScheduledIndexUpdatePreferredTime { get; set; } = Settings.AutomaticIndexUpdatePreferredTime;

        public bool RunScheduledIndexUpdateOnlyWhenIdle { get; set; } = Settings.AutomaticIndexUpdateOnlyWhenIdle;

        public bool IsGlobalHotkeyEnabled { get; set; } = Settings.GlobalHotkeyEnabled;

        public string GlobalHotkeyKeys { get; set; } = Settings.GlobalHotkeyKeys;

        public SettingsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            this.Unloaded += SettingsPage_Unloaded;
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SaveSettings();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            settingsSaved = false;
        }


        private void SaveSettings()
        {
            if (settingsSaved)
            {
                return;
            }
            settingsSaved = true;

            Settings.AutomaticIndexUpdateEnabled = IsScheduledIndexUpdateEnabled;
            Settings.AutomaticIndexUpdateInterval = SelectedScheduledIndexUpdateInterval;
            Settings.AutomaticIndexUpdatePreferredTime = ScheduledIndexUpdatePreferredTime;
            Settings.AutomaticIndexUpdateOnlyWhenIdle = RunScheduledIndexUpdateOnlyWhenIdle;

            Settings.GlobalHotkeyEnabled = IsGlobalHotkeyEnabled;
            Settings.GlobalHotkeyKeys = GlobalHotkeyKeys;

            new SearchIndexService().UpdateDriveList(drives);

            App.MainWindowInstance?.UpdateGlobalHotkey();
        }
       

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnableIndexedSearchCheckBox.IsChecked = Settings.UseIndex;
            EnableIndexedSearchCheckBox.Checked += EnableIndexedSearchCheckBox_CheckedChanged;
            EnableIndexedSearchCheckBox.Unchecked += EnableIndexedSearchCheckBox_CheckedChanged;

            await LoadDrivesAsync();
        }

        private void EnableIndexedSearchCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Settings.UseIndex = EnableIndexedSearchCheckBox.IsChecked ?? false;
        }

        private async Task LoadDrivesAsync()
        {
            DriveLoadingProgressRing.IsActive = true;
            DriveLoadingProgressRing.Visibility = Visibility.Visible;
            DriveLoadingText.Visibility = Visibility.Visible;
            IndexedDrivesListView.Visibility = Visibility.Collapsed;
            StartIndexingButton.IsEnabled = false;

            try
            {
                drives = await Task.Run(() => DriveUtil.GetDrives());
                IndexedDrivesListView.ItemsSource = drives;
            }
            catch
            {
                drives = [];
                IndexedDrivesListView.ItemsSource = drives;
            }
            finally
            {
                DriveLoadingProgressRing.IsActive = false;
                DriveLoadingProgressRing.Visibility = Visibility.Collapsed;
                DriveLoadingText.Visibility = Visibility.Collapsed;
                IndexedDrivesListView.Visibility = Visibility.Visible;
                StartIndexingButton.IsEnabled = true;
            }
        }


        private async void StartIndexingButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DriveIndexingJob job = new DriveIndexingJob(drives)
            {
                Priority = JobPriority.Normal,
                BlocksUI = true
            };
            await JobScheduler.Instance.ScheduleAndWaitAsync(job);
            await LoadDrivesAsync();
        }

        private async void DeleteDriveIndexButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Drive drive)
            {
                return;
            }

            if (drive.StorageDeviceId == 0)
            {
                return;
            }

            button.IsEnabled = false;
            await Task.Run(() =>
            {
                new SearchIndexService().ClearIndexForDevice(drive.StorageDeviceId);
            });

            await LoadDrivesAsync();
        }

        private void RecordHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            _isRecordingHotkey = true;
            RecordHotkeyButton.Content = "Press shortcut...";
        }

        private void RecordHotkeyButton_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (!_isRecordingHotkey)
            {
                return;
            }

            VirtualKey key = e.Key;

            if (key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu
                or VirtualKey.LeftWindows or VirtualKey.RightWindows
                or VirtualKey.LeftControl or VirtualKey.RightControl
                or VirtualKey.LeftShift or VirtualKey.RightShift
                or VirtualKey.LeftMenu or VirtualKey.RightMenu)
            {
                return;
            }

            List<string> parts = [];

            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            var altState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
            var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            var winLState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows);
            var winRState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows);

            if (ctrlState.HasFlag(CoreVirtualKeyStates.Down))
            {
                parts.Add("Ctrl");
            }
            if (altState.HasFlag(CoreVirtualKeyStates.Down))
            {
                parts.Add("Alt");
            }
            if (shiftState.HasFlag(CoreVirtualKeyStates.Down))
            {
                parts.Add("Shift");
            }
            if (winLState.HasFlag(CoreVirtualKeyStates.Down) || winRState.HasFlag(CoreVirtualKeyStates.Down))
            {
                parts.Add("Win");
            }

            string keyName = HotkeyHelper.MapVirtualKeyToName(key);
            if (!string.IsNullOrEmpty(keyName))
            {
                parts.Add(keyName);
                GlobalHotkeyKeys = string.Join("+", parts);
                HotkeyTextBox.Text = GlobalHotkeyKeys;
            }

            _isRecordingHotkey = false;
            RecordHotkeyButton.Content = "Record shortcut";
            e.Handled = true;
        }

        private void RecordHotkeyButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRecordingHotkey)
            {
                _isRecordingHotkey = false;
                RecordHotkeyButton.Content = "Record shortcut";
            }
        }

    }
}
