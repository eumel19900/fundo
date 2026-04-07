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
    public long FileSizeBytes
    {
        get
        {
            long value = (long)FileSizeValueNumberbox.Value;
            return FileSizeUnitCombobox.SelectedIndex switch
            {
                0 => value,
                1 => value * 1024L,
                2 => value * 1024L * 1024L,
                3 => value * 1024L * 1024L * 1024L,
                _ => value * 1024L
            };
        }
    }
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
