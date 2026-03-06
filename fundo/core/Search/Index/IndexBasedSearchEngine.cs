using fundo.core.Search;
using fundo.core.Search.Index.Filter;
using fundo.core.Search.Index.Entity;
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
        public SearchEngine.EngineType Kind => SearchEngine.EngineType.IndexBased;

        public void reset()
        {
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

            SearchIndexContext ctx = SearchIndexStore.CreateContext();

            IQueryable<FileEntity> query = ctx.FileEntities
                .AsNoTracking()
                .Where(f => f.Path.StartsWith(rootPath));

            
            if(searchFilters != null && searchFilters.Count > 0)
            {
                foreach (var filter in searchFilters)
                {
                    if (filter is IndexBasedFilter indexBasedFilter)
                    {
                        query = indexBasedFilter.addQuery(query);
                    }
                }
            }
            

            await foreach (FileEntity entity in query.AsAsyncEnumerable())
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

                SearchResultItem result = new SearchResultItem(fileInfo);
                yield return result;
            }

            ctx.Dispose();
        }
    }
}