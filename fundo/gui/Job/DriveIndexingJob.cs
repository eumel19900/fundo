using fundo.core.Search.Index;
using fundo.tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fundo.gui.Job
{
    /// <summary>
    /// Job that indexes selected drives.
    /// Demonstrates how to use the job framework without async/await.
    /// </summary>
    public class DriveIndexingJob : JobBase
    {
        private readonly List<Drive> _drivesToIndex;

        public override string JobName => "Drive Indexing";

        public DriveIndexingJob(List<Drive> drives)
        {
            _drivesToIndex = drives?.Where(d => d.IsSelected).ToList()
                ?? throw new ArgumentNullException(nameof(drives));
        }

        protected override void Execute()
        {
            //if (_drivesToIndex.Count == 0)
            //{
            //    ReportStatus("No drives selected", "Please select at least one drive to index.");
            //    Sleep(2000);
            //    return;
            //}

            //SearchIndexService indexService = new();

            //// Wire up progress callback
            //indexService.OnProgress = (fileCount, description) =>
            //{
            //    ReportDescription(description);
            //};

            //// Clear existing index first
            //ReportStatus("Preparing", "Clearing existing index...");
            //indexService.clearIndex();

            ////int driveCount = _drivesToIndex.Count;
            ////int currentDrive = 0;

            //foreach (Drive drive in _drivesToIndex)
            //{
            //    ThrowIfCancellationRequested();

            //    currentDrive++;
            //    double baseProgress = ((currentDrive - 1) / (double)driveCount) * 100;

            //    ReportProgress(baseProgress, 100);
            //    ReportStatus($"Indexing drive {drive.DriveLetter}",
            //        $"Processing drive {currentDrive} of {driveCount}...");

            //    // Index this drive (synchronous)
            //    indexService.UpdateDriveIndex(drive, CancellationToken);

            //    double endProgress = (currentDrive / (double)driveCount) * 100;
            //    ReportProgress(endProgress, 100);
            //}

            //ReportProgress(100, 100);
            //ReportStatus("Indexing completed", $"Successfully indexed {driveCount} drive(s).");


            if (_drivesToIndex.Count == 0)
            {
                ReportStatus("No drives selected", "Please select at least one drive to index.");
                return;
            }

            SearchIndexService searchIndexService = new SearchIndexService();
            searchIndexService.OnProgress = (fileCount, description) =>
            {
                ReportDescription(description);
            };

            
            ReportStatus("Preparing", "Clearing existing index...");
            searchIndexService.clearIndex();

            int driveCount = _drivesToIndex.Count;
            int currentDrive = 0;
            foreach (Drive drive in _drivesToIndex)
            {
                ThrowIfCancellationRequested();

                
                ReportStatus($"Indexing drive {drive.DriveLetter}",
                    $"Processing drive {currentDrive} of {driveCount}...");


                searchIndexService.UpdateDriveIndex(drive);

                ReportProgress(currentDrive, driveCount);
            }
            ReportProgress(driveCount, driveCount);

            ReportStatus("Indexing completed!", "");
        }
    }
}
