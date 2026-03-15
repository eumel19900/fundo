using fundo.core.Persistence.Entity;
using fundo.core.Persistence.Filter;
using fundo.core.Search;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace fundo.core.Persistence
{
    internal class IndexBasedSearchEngine : SearchEngine
    {
        public SearchEngine.EngineType Kind => SearchEngine.EngineType.IndexBased;

        public void reset()
        {
        }

        public async IAsyncEnumerable<SearchResultItem> SearchAsync(
            DirectoryInfo startDirectory,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            List<SearchFilter> searchFilters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (startDirectory == null || !startDirectory.Exists)
            {
                yield break;
            }

            string rootPath = startDirectory.FullName;

            List<FileEntity> entities;
            using (SearchIndexContext ctx = SearchIndexStore.CreateContext())
            {
                IQueryable<FileEntity> query = ctx.FileEntities
                    .AsNoTracking()
                    .Where(f => f.Path.StartsWith(rootPath));

                if (searchFilters != null && searchFilters.Count > 0)
                {
                    foreach (var filter in searchFilters)
                    {
                        if (filter is IndexBasedFilter indexBasedFilter)
                        {
                            query = indexBasedFilter.addQuery(query);
                        }
                    }
                }

                entities = await query.ToListAsync(cancellationToken);
            }

            foreach (FileEntity entity in entities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(entity.Path);
                }
                catch
                {
                    continue;
                }

                if (!fileInfo.Exists)
                {
                    continue;
                }

                bool allowed = true;
                if (searchFilters != null && searchFilters.Count > 0)
                {
                    foreach (var filter in searchFilters)
                    {
                        if (filter is NativeSearchFilter nativeSearchFilter)
                        {
                            allowed = nativeSearchFilter.isAllowed(fileInfo);
                            if (!allowed)
                            {
                                break;
                            }
                        }
                    }
                }

                if (!allowed)
                {
                    continue;
                }

                SearchResultItem result = new SearchResultItem(fileInfo);
                yield return result;
            }
        }
    }
}