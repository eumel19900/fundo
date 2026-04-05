using fundo.core.Search.Filter;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace fundo.core.Search.Filter
{
	public class FileNameFilter : INativeSearchFilter
	{
		private readonly string searchPattern;
		private readonly bool useRegex;
		private readonly Regex? regex;

		public FileNameFilter(string searchPattern, bool useRegex = false)
		{
			this.searchPattern = searchPattern ?? string.Empty;
			this.useRegex = useRegex;

			if (useRegex && !string.IsNullOrEmpty(this.searchPattern))
			{
				try
				{
					regex = new Regex(this.searchPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
				}
				catch
				{
					this.useRegex = false; // Fallback to wildcard matching if regex is invalid
				}
			}
		}

		public bool IsAllowed(FileInfo fileInfo)
		{
			if (fileInfo == null) return false;

			// If no pattern provided, allow all
			if (string.IsNullOrEmpty(searchPattern)) return true;

			var name = fileInfo.Name;

			if (useRegex)
			{
				return regex!.IsMatch(name);
			}

			return WildcardMatch(name, searchPattern);
		}

        private static bool WildcardMatch(string text, string pattern)
        {
            // simple wildcard matcher supporting '?' and '*'
            // case-insensitive (Windows file names)
            if (text == null) return false;
            if (pattern == null) return true;

            int t = 0, p = 0;
            int starIdx = -1, match = 0;
            int tLen = text.Length, pLen = pattern.Length;

            while (t < tLen)
            {
                if (p < pLen && (pattern[p] == '?' || char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t])))
                {
                    // single-character match
                    t++; p++;
                }
                else if (p < pLen && pattern[p] == '*')
                {
                    // record star position and the match position in text
                    starIdx = p;
                    match = t;
                    p++; // move pattern past '*'
                }
                else if (starIdx != -1)
                {
                    // last pattern pointer was '*', backtrack: let '*' match one more character
                    p = starIdx + 1;
                    match++;
                    t = match;
                }
                else
                {
                    return false;
                }
            }

            // skip remaining '*' in pattern
            while (p < pLen && pattern[p] == '*') p++;

            return p == pLen;
        }
    }
}
