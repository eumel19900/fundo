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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace fundo.gui;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DateFilterPage : Page
{
    public bool DateFilterEnabled => FilterByDateCheckbox.IsChecked == true;
    public DateTime startTime
    {
        get
        {
            return new DateTime(DateOnly.FromDateTime(FromDatePicker.Date.LocalDateTime), TimeOnly.FromTimeSpan(FromTimePicker.Time));
        }
    }

    public DateTime endTime
    {
        get
        {
            return new DateTime(DateOnly.FromDateTime(ToDatePicker.Date.LocalDateTime), TimeOnly.FromTimeSpan(ToTimePicker.Time));
        }
    }

    public DateFilterPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        FromDatePicker.Date = new DateTimeOffset(new DateTime(1970, 1, 1));
        ToDatePicker.Date = new DateTimeOffset(new DateTime(2026, 12, 31));
    }
}
