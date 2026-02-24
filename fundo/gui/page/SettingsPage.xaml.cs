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
            // Page UI is filled on Loaded to ensure controls are ready.
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetVolumeNameForVolumeMountPoint(string lpszVolumeMountPoint, StringBuilder lpszVolumeName, int cchBufferLength);

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var drives = new List<IndexedDriveEntry>();

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    // Use drive letter (e.g. "C:") to query NT device path.
                    var driveLetter = drive.Name.TrimEnd('\\'); // "C:\" -> "C:"
                    if (string.IsNullOrEmpty(driveLetter))
                    {
                        continue;
                    }

                    var ntPath = GetNtDevicePath(driveLetter);
                    if (string.IsNullOrEmpty(ntPath))
                    {
                        // Fallback to the drive letter if NT path lookup fails.
                        ntPath = driveLetter;
                    }

                    var volumeGuid = GetVolumeGuid(drive.Name);

                    drives.Add(new IndexedDriveEntry(driveLetter, ntPath, volumeGuid));
                }
            }
            catch
            {
                // If anything goes wrong, do not crash settings page; list may stay empty.
            }

            IndexedDrivesListView.ItemsSource = drives;
        }

        private static string GetNtDevicePath(string dosDeviceName)
        {
            const int bufferSize = 1024;
            var sb = new StringBuilder(bufferSize);
            var result = QueryDosDevice(dosDeviceName, sb, bufferSize);

            if (result == 0)
            {
                return string.Empty;
            }

            // QueryDosDevice can return multiple null-terminated strings; take first one.
            var raw = sb.ToString();
            var firstTerminator = raw.IndexOf('\0');
            return firstTerminator >= 0 ? raw.Substring(0, firstTerminator) : raw;
        }

        private static string GetVolumeGuid(string mountPoint)
        {
            const int bufferSize = 1024;
            var sb = new StringBuilder(bufferSize);

            // mountPoint should be like "C:\\"
            var ok = GetVolumeNameForVolumeMountPoint(mountPoint, sb, bufferSize);
            if (!ok)
            {
                return string.Empty;
            }

            var raw = sb.ToString();
            var firstTerminator = raw.IndexOf('\0');
            return firstTerminator >= 0 ? raw.Substring(0, firstTerminator) : raw;
        }
    }

    internal sealed class IndexedDriveEntry
    {
        public string DriveLetter { get; }
        public string NtPath { get; }
        public string VolumeGuid { get; }

        // Indicates whether this drive entry is selected in the UI.
        public bool IsSelected { get; set; }

        public IndexedDriveEntry(string driveLetter, string ntPath, string volumeGuid)
        {
            DriveLetter = driveLetter;
            NtPath = ntPath;
            VolumeGuid = volumeGuid;
        }
    }
}
