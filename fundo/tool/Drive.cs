using System;
using System.Collections.Generic;
using System.Text;

namespace fundo.tool
{
    public class Drive
    {
        public string DriveLetter { get; }
        public string NtPath { get; }
        public string VolumeGuid { get; }

        // Indicates whether this drive entry is selected in the UI.
        public bool IsSelected { get; set; }

        public Drive(string driveLetter, string ntPath, string volumeGuid)
        {
            DriveLetter = driveLetter;
            NtPath = ntPath;
            VolumeGuid = volumeGuid;
        }
    }
}
