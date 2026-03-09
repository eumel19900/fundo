using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace fundo.gui.control
{
    public sealed partial class LocationPickerDialog : ContentDialog
    {
        private readonly ObservableCollection<string> directories = new();

        public List<string> SelectedDirectories { get; private set; } = new();
        public bool Confirmed { get; private set; }

        public LocationPickerDialog()
        {
            InitializeComponent();
            DirectoryListView.ItemsSource = directories;
        }

        public void SetDirectories(IEnumerable<string> dirs)
        {
            directories.Clear();
            foreach (string dir in dirs)
            {
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    directories.Add(dir);
                }
            }
        }

        private void DirectoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = DirectoryListView.SelectedItem != null;
            bool canRemove = hasSelection && directories.Count > 1;
            RemoveButton.IsEnabled = canRemove;
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = App.MainWindowInstance;
                FolderPicker folderPicker = new FolderPicker(window.AppWindow.Id)
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    CommitButtonText = "Select Folder",
                    ViewMode = PickerViewMode.List,
                };

                var result = await folderPicker.PickSingleFolderAsync();

                if (result != null && !string.IsNullOrWhiteSpace(result.Path))
                {
                    AddDirectoryWithHierarchyCheck(result.Path);
                }
            }
            catch
            {
                // Ignore picker errors
            }
        }

        /// <summary>
        /// Adds a directory to the list with hierarchy checks:
        /// - If the new directory is already covered by an existing parent directory, it is not added.
        /// - If the new directory is a parent of existing directories, those child directories are removed.
        /// </summary>
        private void AddDirectoryWithHierarchyCheck(string newPath)
        {
            string normalizedNewPath = NormalizePath(newPath);

            // Check if an existing directory already contains the new path (new path is a child)
            foreach (string existing in directories)
            {
                string normalizedExisting = NormalizePath(existing);
                if (IsSubdirectoryOf(normalizedNewPath, normalizedExisting))
                {
                    // New path is already covered by an existing parent directory - don't add
                    return;
                }
            }

            // Check if the new path is a parent of any existing directories - remove those children
            List<string> toRemove = new();
            foreach (string existing in directories)
            {
                string normalizedExisting = NormalizePath(existing);
                if (IsSubdirectoryOf(normalizedExisting, normalizedNewPath))
                {
                    toRemove.Add(existing);
                }
            }

            foreach (string child in toRemove)
            {
                directories.Remove(child);
            }

            // Check for exact duplicate (case-insensitive)
            bool alreadyExists = directories.Any(d =>
                string.Equals(NormalizePath(d), normalizedNewPath, StringComparison.OrdinalIgnoreCase));

            if (!alreadyExists)
            {
                directories.Add(newPath);
            }
        }

        /// <summary>
        /// Normalizes a path for comparison: ensures trailing separator and uses consistent casing.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalized = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return normalized + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Checks if childPath is a subdirectory of parentPath.
        /// </summary>
        private static bool IsSubdirectoryOf(string childPath, string parentPath)
        {
            return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase) &&
                   childPath.Length > parentPath.Length;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DirectoryListView.SelectedItem is string selected && directories.Count > 1)
            {
                directories.Remove(selected);
            }
        }

        private void AllDrivesButton_Click(object sender, RoutedEventArgs e)
        {
            directories.Clear();

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady)
                    {
                        directories.Add(drive.Name);
                    }
                }
            }
            catch
            {
                // If drive enumeration fails, add at least C:\
                directories.Add(@"C:\");
            }

            if (directories.Count == 0)
            {
                directories.Add(@"C:\");
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (directories.Count == 0)
            {
                args.Cancel = true;
                return;
            }

            Confirmed = true;
            SelectedDirectories = directories.ToList();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Confirmed = false;
        }
    }
}
