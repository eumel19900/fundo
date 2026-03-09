using System;
using Microsoft.UI.Xaml;
using fundo.core.Search;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace fundo.gui;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SizeFilterPage : Page
{
    public long FileSizeKib => (long) FileSizeValueNumberbox.Value;
    public bool SizeFilterEnabled =>
        FilterBySizeCheckbox.IsChecked == true;

    public SizeFilterPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
    }


    public FileSizeCompareMode CompareMode
    {
        get
        {
            return FileSizeTypeCombobox.SelectedIndex switch
            {
                0 => FileSizeCompareMode.Equals,
                1 => FileSizeCompareMode.BiggerThan,
                2 => FileSizeCompareMode.SmallerThan,
                _ => FileSizeCompareMode.Equals
            };
        }
    }
}
