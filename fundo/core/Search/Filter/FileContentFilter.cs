using System;
using System.IO;
using System.Text.RegularExpressions;

namespace fundo.core.Search.Filter
{
    internal class FileContentFilter : INativeSearchFilter
    {
        private readonly string searchText;
        private readonly bool caseSensitive;
        private readonly bool useRegex;
        private readonly bool wholeWord;
        private readonly bool invertMatch;
        private readonly Regex? regex;

        public FileContentFilter(string searchText, bool caseSensitive = false, bool useRegex = false, bool wholeWord = false, bool invertMatch = false)
        {
            this.searchText = searchText ?? string.Empty;
            this.caseSensitive = caseSensitive;
            this.wholeWord = wholeWord;
            this.invertMatch = invertMatch;

            if (useRegex)
            {
                try
                {
                    RegexOptions options = RegexOptions.Compiled;
                    if (!caseSensitive)
                    {
                        options |= RegexOptions.IgnoreCase;
                    }
                    this.regex = new Regex(searchText, options);
                    this.useRegex = true;
                }
                catch
                {
                    this.regex = null;
                    this.useRegex = false;
                }
            }
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

            bool found = useRegex ? SearchWithRegex(fileInfo) : SearchWithText(fileInfo);
            return invertMatch ? !found : found;
        }

        private bool SearchWithRegex(FileInfo fileInfo)
        {
            try
            {
                using StreamReader reader = new StreamReader(fileInfo.FullName);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (regex!.IsMatch(line))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private bool SearchWithText(FileInfo fileInfo)
        {
            try
            {
                StringComparison comparison = caseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                using StreamReader reader = new StreamReader(fileInfo.FullName);
                char[] buffer = new char[4096];
                string remainder = string.Empty;
                int charactersRead;

                while ((charactersRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string contentChunk = remainder + new string(buffer, 0, charactersRead);

                    if (wholeWord)
                    {
                        if (ContainsWholeWord(contentChunk, comparison))
                        {
                            return true;
                        }
                    }
                    else if (contentChunk.IndexOf(searchText, comparison) >= 0)
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

        private bool ContainsWholeWord(string content, StringComparison comparison)
        {
            int index = 0;
            while ((index = content.IndexOf(searchText, index, comparison)) >= 0)
            {
                bool startBoundary = index == 0 || !char.IsLetterOrDigit(content[index - 1]);
                int endIndex = index + searchText.Length;
                bool endBoundary = endIndex >= content.Length || !char.IsLetterOrDigit(content[endIndex]);

                if (startBoundary && endBoundary)
                {
                    return true;
                }

                index++;
            }

            return false;
        }
    }
}
