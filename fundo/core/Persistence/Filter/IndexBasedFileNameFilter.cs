using fundo.core.Persistence.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;

namespace fundo.core.Persistence.Filter
{
    internal class IndexBasedFileNameFilter : IIndexBasedFilter
    {
        private readonly string searchPattern;
        private readonly bool useRegex;

        public IndexBasedFileNameFilter(string searchPattern, bool useRegex = false)
        {
            this.searchPattern = searchPattern ?? string.Empty;
            this.useRegex = useRegex;

            if (useRegex && !string.IsNullOrEmpty(this.searchPattern))
            {
                try
                {
                    _ = new Regex(this.searchPattern);
                }
                catch
                {
                    this.useRegex = false;
                }
            }
        }

        public IQueryable<FileEntity> AddQuery(IQueryable<FileEntity> query)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                return query;
            }

            if (useRegex)
            {
                return query.Where(f => SearchIndexContext.Regexp(searchPattern, f.FileName));
            }

            string likePattern = ConvertWildcardPatternToLike(searchPattern);
            return query.Where(f => EF.Functions.Like(f.FileName, likePattern));
        }

        /// <summary>
        /// Konvertiert ein Windows-Wildcard-Pattern ("*" und "?") in ein SQL-LIKE-Pattern
        /// ("%" und "_"). Andere Zeichen werden 1:1 übernommen.
        /// </summary>
        private static string ConvertWildcardPatternToLike(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return pattern;
            }

            var sb = new System.Text.StringBuilder(pattern.Length);

            foreach (char c in pattern)
            {
                switch (c)
                {
                    case '*':
                        sb.Append('%');
                        break;
                    case '?':
                        sb.Append('_');
                        break;
                    case '%':
                    case '_':
                    case '[':
                    case ']':
                        // Escape LIKE-Sonderzeichen, damit sie wie normale Zeichen behandelt werden.
                        sb.Append('[').Append(c).Append(']');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
