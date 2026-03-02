using fundo;
using fundo.core;
using fundo.core.Search;
using fundo.core.Search;
using fundo.core.Search.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fundo.core.Search
{
    internal interface SearchFilter
    {
        /// <summary>
        /// First check whether this file is allowed. This check should be very fast and not require any access to the filesystem.
        /// If a filter needs to check the file/file-content in filesystem, this can be done in IsAllowedInFileSystemCheck.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public bool isAllowed(FileInfo fileInfo)
        {
            return true;
        }

        /// <summary>
        /// Second check. If a filter needs to check the file/file-content in filesystem, this can be done here
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public bool IsAllowedInFileSystemCheck(FileInfo fileInfo)
        {
            return true;
        }
    }
}
