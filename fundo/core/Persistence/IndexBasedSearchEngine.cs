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
    internal class IndexBasedSearchEngine : ISearchEngine
    {
        public ISearchEngine.EngineType Kind => ISearchEngine.EngineType.IndexBased;

        public void Reset()
        {
        }

        public async IAsyncEnumerable<DetachedFileInfo> SearchAsync(
            DirectoryInfo startDirectory,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            List<ISearchFilter> searchFilters)
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
                        if (filter is IIndexBasedFilter indexBasedFilter)
                        {
                            query = indexBasedFilter.AddQuery(query);
                        }
                    }
                }

                entities = await query.ToListAsync(cancellationToken);
            }

            foreach (FileEntity entity in entities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileInfo fileInfo;
                fileInfo = new FileInfo(entity.Path);

                bool allowed = true;
                if (searchFilters != null && searchFilters.Count > 0)
                {
                    foreach (var filter in searchFilters)
                    {
                        if (filter is INativeSearchFilter nativeSearchFilter)
                        {
                            allowed = nativeSearchFilter.IsAllowed(fileInfo);
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

                DetachedFileInfo result = new DetachedFileInfo(entity);
                yield return result;
            }
        }
    }
}