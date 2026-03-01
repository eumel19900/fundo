using fundo.core.Search.Index.Entity;
using fundo.core.Search.Native;
using fundo.tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public void clearIndex()
        {
            SearchIndexStore.DeleteAllFiles();
        }

        public async Task UpdateDriveIndexAsync(Drive drive, CancellationToken cancellationToken = default)
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
            searchEngine.LoadFileIcons = false;

            await foreach (SearchResultItem result in searchEngine.SearchAsync(
                new DirectoryInfo(drive.DriveLetter),
                cancellationToken, 
                null))
            {
                FileEntity fileEntity = new FileEntity
                {
                    FileName = result.FileName,
                    Path = result.Path,
                    FileSize = result.FileSize,
                    FileDate = result.FileDate,
                    StorageDeviceId = storageDeviceId,
                };

                batch.Add(fileEntity);

                // Batch ist voll -> SYNCHRON in DB persistieren
                if (batch.Count >= batchSize)
                {
                    SearchIndexStore.AddFilesBulk(batch);
                    batch.Clear();
                }
            }

            // Restliche Objekte persistieren (falls < 10000)
            if (batch.Count > 0)
            {
                SearchIndexStore.AddFilesBulk(batch);
                batch.Clear();
            }
        }
    }
}
