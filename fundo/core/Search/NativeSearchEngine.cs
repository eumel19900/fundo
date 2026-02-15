using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fundo.core.Search
{
    internal class NativeSearchEngine : SearchEngine
    {
        async IAsyncEnumerable<SearchResult> SearchEngine.SearchAsync(DirectoryInfo startDirectory, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for(int i = 0; i < 100000; i++)
            {
                await Task.Yield();

                yield return new SearchResult("C:\\RobertsDatei." + i + ".txt", i, new DateTime());

                //await Task.Delay(250, cancellationToken);
            }

            /*foreach (var file in Directory.EnumerateFiles("C:\\", "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (file.Contains("blablub"))
                {
                    yield return new SearchResult();
                }

                // optional: async work simulieren
                await Task.Yield();
            }*/
        }
    }
}
