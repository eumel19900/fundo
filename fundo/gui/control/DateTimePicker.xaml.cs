using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace fundo.gui.control
{
    public sealed partial class DateTimePicker : UserControl
    {
        private bool isUpdatingControls;
        private bool isInitialized;

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(
                nameof(Time),
                typeof(DateTime),
                typeof(DateTimePicker),
                new PropertyMetadata(default(DateTime), OnTimePropertyChanged));

        public static readonly DependencyProperty DefaultTimeProperty =
            DependencyProperty.Register(
                nameof(DefaultTime),
                typeof(DateTime),
                typeof(DateTimePicker),
                new PropertyMetadata(default(DateTime), OnDefaultTimePropertyChanged));

        public DateTime Time
        {
            get => (DateTime)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public DateTime DefaultTime
        {
            get => (DateTime)GetValue(DefaultTimeProperty);
            set => SetValue(DefaultTimeProperty, value);
        }

        public DateTimePicker()
        {
            InitializeComponent();
            Loaded += DateTimePicker_Loaded;
        }

        private void DateTimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                return;
            }

            DateTime initialTime = Time != default
                ? Time
                : (DefaultTime != default ? DefaultTime : DateTime.Now);

            ApplyDateTimeToControls(initialTime);
            Time = initialTime;
            isInitialized = true;
        }

        private static void OnTimePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimePicker control = (DateTimePicker)d;
            if (control.isUpdatingControls)
            {
                return;
            }

            DateTime newValue = (DateTime)e.NewValue;
            if (newValue == default)
            {
                return;
            }

            control.ApplyDateTimeToControls(newValue);
        }

        private static void OnDefaultTimePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimePicker control = (DateTimePicker)d;
            if (control.isInitialized)
            {
                return;
            }

            DateTime newValue = (DateTime)e.NewValue;
            if (newValue == default)
            {
                return;
            }

            control.ApplyDateTimeToControls(newValue);
            control.Time = newValue;
        }

        private void InnerDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs args)
        {
            UpdateTimeFromControls();
        }

        private void InnerTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs args)
        {
            UpdateTimeFromControls();
        }

        private void UpdateTimeFromControls()
        {
            if (isUpdatingControls)
            {
                return;
            }

            isUpdatingControls = true;
            try
            {
                Time = new DateTime(
                    DateOnly.FromDateTime(InnerDatePicker.Date.LocalDateTime),
                    TimeOnly.FromTimeSpan(InnerTimePicker.Time));
            }
            finally
            {
                isUpdatingControls = false;
            }
        }

        private void ApplyDateTimeToControls(DateTime value)
        {
            isUpdatingControls = true;
            try
            {
                InnerDatePicker.Date = new DateTimeOffset(value.Date);
                InnerTimePicker.Time = value.TimeOfDay;
            }
            finally
            {
                isUpdatingControls = false;
            }
        }
    }
}
