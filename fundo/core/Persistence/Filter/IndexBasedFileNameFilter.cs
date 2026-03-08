using fundo.core.Persistence.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace fundo.core.Persistence.Filter
{
    internal class IndexBasedFileNameFilter : IndexBasedFilter
    {
        private readonly string searchPattern;

        public IndexBasedFileNameFilter(string searchPattern)
        {
            this.searchPattern = searchPattern ?? string.Empty;
        }

        public IQueryable<FileEntity> addQuery(IQueryable<FileEntity> query)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                // Kein Pattern gesetzt -> Query unverändert lassen.
                return query;
            }

            string likePattern = ConvertWildcardPatternToLike(searchPattern);

            // Verwende EF.Functions.Like, damit die Filterung von der Datenbank
            // (und damit ggf. vom Index) ausgewertet werden kann.
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
