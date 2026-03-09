using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fundo.gui.control
{
    public sealed partial class LocationControl : UserControl
    {
        private List<string> directories = new() { @"C:\" };
        private bool isUpdatingText;

        public event TextChangedEventHandler SelectedDirectoryChanged;

        /// <summary>
        /// Gets or sets the first (or only) directory. For backwards compatibility.
        /// </summary>
        public string SelectedDirectory
        {
            get => directories.FirstOrDefault() ?? string.Empty;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    directories = new List<string> { value };
                    UpdateTextBoxDisplay();
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of directories.
        /// </summary>
        public List<string> Directories
        {
            get => new List<string>(directories);
            set
            {
                if (value != null && value.Count > 0)
                {
                    directories = new List<string>(value);
                }
                else
                {
                    directories = new List<string> { @"C:\" };
                }
                UpdateTextBoxDisplay();
            }
        }

        /// <summary>
        /// Initial directory to use (can be set in XAML).
        /// </summary>
        public string InitialDirectory { get; set; } = @"C:\";

        /// <summary>
        /// Returns true if multiple directories are selected.
        /// </summary>
        public bool HasMultipleDirectories => directories.Count > 1;

        /// <summary>
        /// Returns all selected directories as DirectoryInfo objects.
        /// </summary>
        public IEnumerable<DirectoryInfo> GetDirectoryInfos()
        {
            foreach (string dir in directories)
            {
                DirectoryInfo? info = null;
                try
                {
                    info = new DirectoryInfo(dir);
                }
                catch
                {
                    // Skip invalid paths
                }
                if (info != null)
                {
                    yield return info;
                }
            }
        }

        public LocationControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(InitialDirectory) && directories.Count == 1 && directories[0] == @"C:\")
            {
                directories[0] = InitialDirectory;
            }
            UpdateTextBoxDisplay();
        }

        private void UpdateTextBoxDisplay()
        {
            isUpdatingText = true;
            try
            {
                if (directories.Count == 1)
                {
                    LocationTextBox.Text = directories[0];
                    LocationTextBox.IsReadOnly = false;
                    LocationTextBox.PlaceholderText = @"C:\";
                }
                else
                {
                    LocationTextBox.Text = string.Join("; ", directories);
                    LocationTextBox.IsReadOnly = true;
                    LocationTextBox.PlaceholderText = "Multiple directories selected";
                }
            }
            finally
            {
                isUpdatingText = false;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LocationPickerDialog dialog = new LocationPickerDialog
                {
                    XamlRoot = this.XamlRoot
                };
                dialog.SetDirectories(directories);

                await dialog.ShowAsync();

                if (dialog.Confirmed && dialog.SelectedDirectories.Count > 0)
                {
                    directories = dialog.SelectedDirectories;
                    UpdateTextBoxDisplay();
                    SelectedDirectoryChanged?.Invoke(this, null);
                }
            }
            catch
            {
                // Ignore dialog errors
            }
        }

        private void LocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingText)
            {
                return;
            }

            // Only update the single directory if we have exactly one
            if (directories.Count == 1 && !LocationTextBox.IsReadOnly)
            {
                directories[0] = LocationTextBox.Text;
            }

            SelectedDirectoryChanged?.Invoke(this, e);
        }
    }
}
