using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace fundo.gui.control
{
    public sealed partial class LocationControl : UserControl
    {
        public event TextChangedEventHandler SelectedDirectoryChanged;

        public string SelectedDirectory
        {
            get => LocationTextBox.Text;
            set => LocationTextBox.Text = value;
        }

        public LocationControl()
        {
            InitializeComponent();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = App.MainWindowInstance;
            FolderPicker folderPicker = new FolderPicker(window.AppWindow.Id)
            {
                // (Optional) Specify the initial location for the picker. 
                //     If the specified location doesn't exist on the user's machine, it falls back to the DocumentsLibrary.
                //     If not set, it defaults to PickerLocationId.Unspecified, and the system will use its default location.
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,

                // (Optional) specify the text displayed on the commit button. 
                //     If not specified, the system uses a default label of "Open" (suitably translated).
                CommitButtonText = "Select Folder",

                // (Optional) specify the view mode of the picker dialog. If not specified, default to List.
                ViewMode = PickerViewMode.List,
            };

            var result = await folderPicker.PickSingleFolderAsync();

            if (result is not null)
            {
                var path = result.Path;
                LocationTextBox.Text = path;
            }
        }

        private void LocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SelectedDirectoryChanged?.Invoke(this, e);
        }
    }
}
