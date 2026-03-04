using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace fundo.gui;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AttributeFilterPage : Page
{
  

    // Nur lesender Zugriff auf die Checkbox-Zustände

    public bool FilterByFileAttributesEnabled =>
        FilterByFileAttributesCheckbox.IsChecked == true;

    public bool IsReadonlyChecked =>
        ReadonlyCheckbox.IsChecked == true;

    public bool IsHiddenChecked =>
        HiddenCheckbox.IsChecked == true;

    public bool IsSystemChecked =>
        SystemCheckbox.IsChecked == true;

    public bool IsArchiveChecked =>
        ArchiveCheckbox.IsChecked == true;

    public bool IsTempChecked =>
        TempCheckbox.IsChecked == true;

    public bool IsCompressedChecked =>
        CompressedCheckbox.IsChecked == true;

    public bool IsEncryptedChecked =>
        EncryptedCheckbox.IsChecked == true;

    public AttributeFilterPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
    }
}
