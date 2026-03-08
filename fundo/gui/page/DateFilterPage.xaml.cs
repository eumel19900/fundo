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
using fundo.gui.control;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace fundo.gui;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DateFilterPage : Page
{
    public bool DateFilterEnabled => FilterByDateCheckbox.IsChecked == true;
    public bool CreationTimeEnabled => DateTypeComboBox.SelectedIndex == 0;
    public bool ModifiedTimeEnabled => DateTypeComboBox.SelectedIndex == 1;
    public bool LastAccessTimeEnabled => DateTypeComboBox.SelectedIndex == 2;

    public DateTime startTime
    {
        get
        {
            return FromDateTimePicker.Time;
        }
    }

    public DateTime endTime
    {
        get
        {
            return ToDateTimePicker.Time;
        }
    }

    public DateFilterPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        FromDateTimePicker.DefaultTime = new DateTime(1970, 1, 1, 0, 0, 0);
        ToDateTimePicker.DefaultTime = new DateTime(2026, 12, 31, 23, 59, 59);
        DateTypeComboBox.SelectedIndex = 0;
    }
}
