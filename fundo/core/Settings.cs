using fundo.core.Persistence;
using System;

namespace fundo.core
{
    internal class Settings
    {
        private const string UseIndexKey = "UseIndex";
        private const string AutomaticIndexUpdateEnabledKey = "AutomaticIndexUpdateEnabled";
        private const string AutomaticIndexUpdateIntervalKey = "AutomaticIndexUpdateInterval";
        private const string AutomaticIndexUpdatePreferredTimeKey = "AutomaticIndexUpdatePreferredTime";
        private const string AutomaticIndexUpdateOnlyWhenIdleKey = "AutomaticIndexUpdateOnlyWhenIdle";
        private const string TrueValue = "true";
        private const string FalseValue = "false";

        public static bool UseIndex
        {
            get
            {
                return GetBooleanValue(UseIndexKey);
            }
            set
            {
                SetBooleanValue(UseIndexKey, value);
            }
        }

        public static bool AutomaticIndexUpdateEnabled
        {
            get
            {
                return GetBooleanValue(AutomaticIndexUpdateEnabledKey);
            }
            set
            {
                SetBooleanValue(AutomaticIndexUpdateEnabledKey, value);
            }
        }

        public static ScheduledIndexUpdateInterval AutomaticIndexUpdateInterval
        {
            get
            {
                string? value = SearchIndexStore.GetPropertyValue(AutomaticIndexUpdateIntervalKey);
                if (Enum.TryParse(value, out ScheduledIndexUpdateInterval interval))
                {
                    return interval;
                }

                return ScheduledIndexUpdateInterval.Daily;
            }
            set
            {
                SearchIndexStore.SetPropertyValue(AutomaticIndexUpdateIntervalKey, value.ToString());
            }
        }

        public static TimeSpan AutomaticIndexUpdatePreferredTime
        {
            get
            {
                string? value = SearchIndexStore.GetPropertyValue(AutomaticIndexUpdatePreferredTimeKey);
                if (TimeSpan.TryParse(value, out TimeSpan preferredTime))
                {
                    return preferredTime;
                }

                return new TimeSpan(2, 0, 0);
            }
            set
            {
                SearchIndexStore.SetPropertyValue(AutomaticIndexUpdatePreferredTimeKey, value.ToString("c"));
            }
        }

        public static bool AutomaticIndexUpdateOnlyWhenIdle
        {
            get
            {
                return GetBooleanValue(AutomaticIndexUpdateOnlyWhenIdleKey);
            }
            set
            {
                SetBooleanValue(AutomaticIndexUpdateOnlyWhenIdleKey, value);
            }
        }

        private static bool GetBooleanValue(string key)
        {
            string? value = SearchIndexStore.GetPropertyValue(key);
            return value == TrueValue;
        }

        private static void SetBooleanValue(string key, bool value)
        {
            SearchIndexStore.SetPropertyValue(key, value ? TrueValue : FalseValue);
        }
    }
}
