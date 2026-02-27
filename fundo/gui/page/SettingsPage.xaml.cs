using fundo.core;
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
        public SettingsPage()
        {
            InitializeComponent();
        }
       

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            IndexedDrivesListView.ItemsSource = DriveUtil.GetDrives();

            EnableIndexedSearchCheckBox.IsChecked = Settings.UseIndex;
            EnableIndexedSearchCheckBox.Checked += (s, args)
                => Settings.UseIndex = (bool)EnableIndexedSearchCheckBox.IsChecked;
        }


        private async void StartIndexingButton_Click(object sender, RoutedEventArgs e)
        {
           
        }
    }
}
