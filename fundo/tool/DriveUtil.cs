using fundo.core.Search.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using fundo.core.Search.Index.Entity;

namespace fundo.tool
{
    internal class DriveUtil
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetVolumeNameForVolumeMountPoint(string lpszVolumeMountPoint, StringBuilder lpszVolumeName, int cchBufferLength);

        public static List<Drive> GetDrives()
        {
            List<Drive> drives = new List<Drive>();

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

                    Drive myDrive = new(driveLetter, ntPath, volumeGuid);
                    StorageDevice? storageDevice = SearchIndexStore.GetStorageDeviceByStorageName(ntPath);
                    if (storageDevice != null)
                    {
                        myDrive.IsSelected = true;
                    }
                    drives.Add(myDrive);
                }
            }
            catch
            {
                // If anything goes wrong, do not crash
            }

            return drives;
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
}
