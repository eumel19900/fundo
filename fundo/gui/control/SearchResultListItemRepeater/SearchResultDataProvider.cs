using fundo.core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fundo.gui.control
{
    /// <summary>
    /// Sort criteria for search results.
    /// </summary>
    internal enum SearchResultSortField
    {
        FileName,
        Directory,
        FileSize,
        FileDate,
        FileType
    }

    /// <summary>
    /// Sort direction.
    /// </summary>
    internal enum SearchResultSortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// High-performance data provider for millions of SearchResultItem objects.
    /// Supports sorting and filtering with index-based access for virtualization.
    /// </summary>
    internal class SearchResultDataProvider
    {
        private List<DetachedFileInfo> _allItems = new();
        private int[] _viewIndices = Array.Empty<int>();

        private string _fileNameFilter = string.Empty;
        private SearchResultSortField _sortField = SearchResultSortField.FileName;
        private SearchResultSortDirection _sortDirection = SearchResultSortDirection.Ascending;

        /// <summary>
        /// Total number of items before filtering.
        /// </summary>
        public int TotalCount => _allItems.Count;

        /// <summary>
        /// Number of items in the current filtered/sorted view.
        /// </summary>
        public int Count => _viewIndices.Length;

        /// <summary>
        /// Current sort field.
        /// </summary>
        public SearchResultSortField SortField => _sortField;

        /// <summary>
        /// Current sort direction.
        /// </summary>
        public SearchResultSortDirection SortDirection => _sortDirection;

        /// <summary>
        /// Current file name filter text.
        /// </summary>
        public string FileNameFilter => _fileNameFilter;

        /// <summary>
        /// Raised when the view (filter/sort/data) changes.
        /// </summary>
        public event EventHandler? ViewChanged;

        /// <summary>
        /// Gets the item at the given view index.
        /// </summary>
        public DetachedFileInfo GetAt(int viewIndex)
        {
            return _allItems[_viewIndices[viewIndex]];
        }

        /// <summary>
        /// Replaces all items with the given collection.
        /// </summary>
        public void SetItems(IReadOnlyList<DetachedFileInfo> items)
        {
            _allItems = new List<DetachedFileInfo>(items);
            RebuildView();
        }

        /// <summary>
        /// Clears all items.
        /// </summary>
        public void Clear()
        {
            _allItems.Clear();
            _viewIndices = Array.Empty<int>();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the file name filter (matches against file name only, case-insensitive).
        /// </summary>
        public void SetFileNameFilter(string filter)
        {
            string trimmed = (filter ?? string.Empty).Trim();
            if (string.Equals(_fileNameFilter, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _fileNameFilter = trimmed;
            RebuildView();
        }

        /// <summary>
        /// Sets the sort field and direction. If the same field is set again, direction toggles.
        /// </summary>
        public void SetSort(SearchResultSortField field)
        {
            if (_sortField == field)
            {
                _sortDirection = _sortDirection == SearchResultSortDirection.Ascending
                    ? SearchResultSortDirection.Descending
                    : SearchResultSortDirection.Ascending;
            }
            else
            {
                _sortField = field;
                _sortDirection = SearchResultSortDirection.Ascending;
            }

            RebuildView();
        }

        /// <summary>
        /// Sets sort field and direction explicitly.
        /// </summary>
        public void SetSort(SearchResultSortField field, SearchResultSortDirection direction)
        {
            if (_sortField == field && _sortDirection == direction)
            {
                return;
            }

            _sortField = field;
            _sortDirection = direction;
            RebuildView();
        }

        private void RebuildView()
        {
            IEnumerable<int> indices = Enumerable.Range(0, _allItems.Count);

            if (!string.IsNullOrEmpty(_fileNameFilter))
            {
                string filter = _fileNameFilter;
                indices = indices.Where(i =>
                    _allItems[i].Name != null &&
                    _allItems[i].Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            int[] filtered = indices.ToArray();

            Array.Sort(filtered, (a, b) =>
            {
                int cmp = CompareItems(_allItems[a], _allItems[b]);
                return _sortDirection == SearchResultSortDirection.Descending ? -cmp : cmp;
            });

            _viewIndices = filtered;
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        private int CompareItems(DetachedFileInfo x, DetachedFileInfo y)
        {
            return _sortField switch
            {
                SearchResultSortField.FileName => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase),
                SearchResultSortField.Directory => string.Compare(
                    x.DirectoryName ?? string.Empty,
                    y.DirectoryName ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase),
                SearchResultSortField.FileSize => x.Length.CompareTo(y.Length),
                SearchResultSortField.FileDate => x.CreationTime.CompareTo(y.CreationTime),
                SearchResultSortField.FileType => string.Compare(
                    x.Extension ?? string.Empty,
                    y.Extension ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }
    }
}
