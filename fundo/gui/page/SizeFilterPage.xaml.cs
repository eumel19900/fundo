using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace fundo.gui;

public enum FileSizeCompareMode
{
    Equals = 0,
    BiggerThan = 1,
    SmallerThan = 2
}

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SizeFilterPage : Page
{
    public SizeFilterPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
    }

    // Nur lesender Zugriff auf Checkbox, Combobox und NumberBox

    public bool SizeFilterEnabled =>
        FilterBySizeCheckbox.IsChecked == true;

    public FileSizeCompareMode CompareMode
    {
        get
        {
            // Combobox-Einträge sind in XAML in exakt dieser Reihenfolge definiert
            return FileSizeTypeCombobox.SelectedIndex switch
            {
                0 => FileSizeCompareMode.Equals,
                1 => FileSizeCompareMode.BiggerThan,
                2 => FileSizeCompareMode.SmallerThan,
                _ => FileSizeCompareMode.Equals
            };
        }
    }

    public double FileSizeKib =>
        FileSizeValueNumberbox.Value;
}
