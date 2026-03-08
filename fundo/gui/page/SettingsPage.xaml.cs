using fundo.core;
using fundo.core.Persistence;
using fundo.gui.Job;
using fundo.tool;
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
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        public SettingsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            drives = DriveUtil.GetDrives();
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

        private void SaveSettings()
        {
            if (settingsSaved)
            {
                return;
            }
            settingsSaved = true;

            new SearchIndexService().updateDriveList(drives);
        }
       

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            IndexedDrivesListView.ItemsSource = drives;

            EnableIndexedSearchCheckBox.IsChecked = Settings.UseIndex;
            EnableIndexedSearchCheckBox.Checked += EnableIndexedSearchCheckBox_CheckedChanged;
            EnableIndexedSearchCheckBox.Unchecked += EnableIndexedSearchCheckBox_CheckedChanged;
        }

        private void EnableIndexedSearchCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Settings.UseIndex = EnableIndexedSearchCheckBox.IsChecked ?? false;
        }


        private async void StartIndexingButton_Click(object sender, RoutedEventArgs e)
        {
            /*var progressDialog = new IndexingGuiService(this.XamlRoot, drives);
            new SearchIndexService().updateDriveList(drives);
            await progressDialog.StartIndexingAsync();*/

            DriveIndexingJob job = new DriveIndexingJob(drives)
            {
                Priority = JobPriority.Normal,
                BlocksUI = true
            };
            await JobScheduler.Instance.ScheduleAndWaitAsync(job);
        }
    }
}
