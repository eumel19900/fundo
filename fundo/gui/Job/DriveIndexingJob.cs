using fundo.core.Persistence;
using fundo.tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
