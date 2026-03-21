using fundo.core.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace fundo.gui.control
{
    /// <summary>
    /// Observable wrapper around SearchResultDataProvider for use with ItemsRepeater.
    /// Implements IReadOnlyList for index-based virtualization and INotifyCollectionChanged for updates.
    /// </summary>
    internal sealed class SearchResultDataView : IReadOnlyList<SearchResultItem>, INotifyCollectionChanged
    {
        private readonly SearchResultDataProvider _provider;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public SearchResultDataView(SearchResultDataProvider provider)
        {
            _provider = provider;
            _provider.ViewChanged += OnProviderViewChanged;
        }

        private void OnProviderViewChanged(object? sender, EventArgs e)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public SearchResultItem this[int index] => _provider.GetAt(index);

        public int Count => _provider.Count;

        public IEnumerator<SearchResultItem> GetEnumerator()
        {
            for (int i = 0; i < _provider.Count; i++)
            {
                yield return _provider.GetAt(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
