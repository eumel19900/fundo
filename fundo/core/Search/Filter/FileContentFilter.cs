using System;
using System.IO;

namespace fundo.core.Search.Filter
{
    internal class FileContentFilter : INativeSearchFilter
    {
        private readonly string searchText;

        public FileContentFilter(string searchText)
        {
            this.searchText = searchText ?? string.Empty;
        }

        public bool IsAllowed(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return false;
            }

            try
            {
                using StreamReader reader = new StreamReader(fileInfo.FullName);
                char[] buffer = new char[4096];
                string remainder = string.Empty;
                int charactersRead;

                while ((charactersRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string contentChunk = remainder + new string(buffer, 0, charactersRead);
                    if (contentChunk.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    int remainderLength = Math.Min(searchText.Length - 1, contentChunk.Length);
                    remainder = remainderLength > 0
                        ? contentChunk.Substring(contentChunk.Length - remainderLength, remainderLength)
                        : string.Empty;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
