using fundo;
using fundo.core;
using fundo.core.Persistence;
using fundo.core.Persistence.Entity;
using fundo.core.Search;
using fundo.tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace fundo.core.Persistence
{
    internal class SearchIndexService
    {
        /// <summary>
        /// Optional callback for reporting progress during indexing.
        /// Parameters: (currentFiles, description)
        /// </summary>
        public Action<long, string>? OnProgress { get; set; }

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

        public void clearIndex()
        {
            SearchIndexStore.DeleteAllFiles();
        }

        public void UpdateDriveIndex(Drive drive, CancellationToken cancellationToken = default)
        {
            StorageDevice? storageDevice = SearchIndexStore.GetStorageDeviceByStorageName(drive.NtPath);
            if (storageDevice == null)
            {
                //drive not found in Database?!
                return;
            }
            long storageDeviceId = storageDevice.Id;
            storageDevice = null;


            const int batchSize = 10000;
            List<FileEntity> batch = new List<FileEntity>(batchSize);

            NativeSearchEngine searchEngine = new NativeSearchEngine();
            searchEngine.reset();

            foreach (DetachedFileInfo result in searchEngine.Search(
                new DirectoryInfo(drive.DriveLetter),
                null))
            {
                FileEntity fileEntity = new FileEntity
                {
                    FileName = result.Name,
                    Path = result.FullName,
                    FileSize = result.Length,
                    CreationTime = result.CreationTime,
                    ModifiedTime = result.LastWriteTime,
                    LastAccessTime = result.LastAccessTime,
                    FileAttributes = FileAttributeHelper.FromSystemFileAttributes(result.Attributes),
                    StorageDeviceId = storageDeviceId,
                };

                batch.Add(fileEntity);

                if (batch.Count >= batchSize)
                {
                    SearchIndexStore.AddFilesBulk(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                SearchIndexStore.AddFilesBulk(batch);
                batch.Clear();
            }
        }
    }
}
