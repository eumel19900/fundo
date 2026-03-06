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

        /// <summary>
        /// Synchronous version of drive indexing.
        /// </summary>
       /* public void UpdateDriveIndex(Drive drive, CancellationToken cancellationToken = default)
        {
            StorageDevice? storageDevice = SearchIndexStore.GetStorageDeviceByStorageName(drive.NtPath);
            if (storageDevice == null)
            {
                return;
            }
            long storageDeviceId = storageDevice.Id;
            storageDevice = null;

            const int batchSize = 10000;
            List<FileEntity> batch = new List<FileEntity>(batchSize);
            long totalFiles = 0;

            // Use synchronous file enumeration
            DirectoryInfo rootDir = new DirectoryInfo(drive.DriveLetter);
            if (!rootDir.Exists)
            {
                return;
            }

            EnumerateFilesRecursive(rootDir, batch, ref totalFiles, storageDeviceId, batchSize, cancellationToken);

            // Persist remaining batch
            if (batch.Count > 0)
            {
                SearchIndexStore.AddFilesBulk(batch);
                batch.Clear();
            }
        }*/

        private void EnumerateFilesRecursive(
            DirectoryInfo directory,
            List<FileEntity> batch,
            ref long totalFiles,
            long storageDeviceId,
            int batchSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Enumerate files in current directory
            IEnumerable<FileInfo> files;
            try
            {
                files = directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException) { return; }
            catch (DirectoryNotFoundException) { return; }
            catch (IOException) { return; }

            foreach (FileInfo file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    FileEntity fileEntity = new FileEntity
                    {
                        FileName = file.Name,
                        Path = file.FullName,
                        FileSize = file.Length,
                        FileDate = file.CreationTime,
                        StorageDeviceId = storageDeviceId,
                    };

                    batch.Add(fileEntity);
                    totalFiles++;

                    if (batch.Count >= batchSize)
                    {
                        SearchIndexStore.AddFilesBulk(batch);
                        batch.Clear();
                        OnProgress?.Invoke(totalFiles, $"Indexed {totalFiles:N0} files...");
                    }
                }
                catch
                {
                    // Skip files that cannot be accessed
                    continue;
                }
            }

            // Recurse into subdirectories
            IEnumerable<DirectoryInfo> subdirs;
            try
            {
                subdirs = directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException) { return; }
            catch (DirectoryNotFoundException) { return; }
            catch (IOException) { return; }

            foreach (DirectoryInfo subdir in subdirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnumerateFilesRecursive(subdir, batch, ref totalFiles, storageDeviceId, batchSize, cancellationToken);
            }
        }

        /// <summary>
        /// Async version of drive indexing (kept for compatibility).
        /// </summary>
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
            searchEngine.LoadFileIcons = false;

            foreach (SearchResultItem result in searchEngine.Search(
                new DirectoryInfo(drive.DriveLetter),
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
