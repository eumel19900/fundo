using System;
using System.IO;
using fundo;
using fundo.core;
using fundo.core.Search;
using fundo.core.Search.Filter;
using fundo.core.Search.Native;

namespace fundo.core.Search.Filter
{
    internal abstract class BaseDateFilter : SearchFilter
    {
        protected readonly DateTime startTime;
        protected readonly DateTime endTime;

        protected BaseDateFilter(DateTime startTime, DateTime endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
        }

        // SearchFilter-Methode – wird in der abgeleiteten Klasse implementiert
        public abstract bool isAllowed(FileInfo fileInfo);
    }
}