using fundo.core.Search.Index.Entity;
using fundo.core.Search.Native;
using fundo.tool;
using System;
using System.Collections.Generic;
using System.Text;

namespace fundo.core.Search.Index
{
    internal class SearchIndexService
    {
        public SearchIndexService()
        {

        }

        public void updateDriveList(List<Drive> drives)
        {
            if (drives == null || drives.Count == 0)
            {
                return;
            }

            foreach (Drive drive in drives)
            {
                StorageDevice storageDevice = SearchIndexStore.GetStorageDeviceByStorageName(drive.NtPath);
                if (storageDevice != null)
                {
                    if (!drive.IsSelected)
                    {   //drive is not selected => delete this drive and its files from index
                        SearchIndexStore.DeleteAllFilesInStorageDevice(storageDevice.Id);
                        SearchIndexStore.DeleteStorageDevice(storageDevice.Id);
                    }
                }
                else
                {
                    if (drive.IsSelected)
                    {
                        SearchIndexStore.GetOrCreateStorageDevice(drive.NtPath);
                    }
                }
            }
        }
    }
}
