using fundo.core.Search;
using fundo.core.Search.Native;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace fundo.core.Search.Index
{
    internal class IndexBasedSearchEngine : SearchEngine
    {
        public void reset()
        {
            // Aktuell kein interner Zustand zu resetten.
        }

        public async IAsyncEnumerable<SearchResultItem> SearchAsync(
            DirectoryInfo startDirectory,
            System.Threading.CancellationToken cancellationToken,
            List<SearchFilter> searchFilters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (startDirectory == null || !startDirectory.Exists)
            {
                yield break;
            }

            string rootPath = startDirectory.FullName;

            using var ctx = SearchIndexStore.CreateContext();

            // Alle indexierten Dateien unterhalb des Startpfads holen
            var query = ctx.FileEntities
                .AsNoTracking()
                .Where(f => f.Path.StartsWith(rootPath));

            // TODO: SearchFilter (FileNameFilter, DateFilter, ...) index-basiert auswerten.

            await foreach (var entity in query.AsAsyncEnumerable())
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(entity.Path);
                }
                catch
                {
                    // Pfad ungültig oder Datei existiert nicht mehr -> Eintrag überspringen
                    continue;
                }

                var result = new SearchResultItem(fileInfo);
                yield return result;
            }
        }
    }
}