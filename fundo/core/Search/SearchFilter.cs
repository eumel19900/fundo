using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fundo.core.Search
{
    internal interface SearchFilter
    {
        public bool isAllowed(FileInfo fileInfo);
    }
}
