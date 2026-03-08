using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using fundo.core.Persistence;
using fundo.core.Persistence.Entity;

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
                    // Für Dateisystem: "C:\" beibehalten
                    var driveLetterForFs = drive.Name;          // "C:\"

                    // Für QueryDosDevice: "C:"
                    var driveLetterForNt = drive.Name.TrimEnd('\\'); // "C:\" -> "C:"
                    if (string.IsNullOrEmpty(driveLetterForNt))
                    {
                        continue;
                    }

                    var ntPath = GetNtDevicePath(driveLetterForNt);
                    if (string.IsNullOrEmpty(ntPath))
                    {
                        ntPath = driveLetterForNt;
                    }

                    var volumeGuid = GetVolumeGuid(drive.Name);

                    Drive myDrive = new(driveLetterForFs, ntPath, volumeGuid);
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
