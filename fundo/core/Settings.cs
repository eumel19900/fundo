using fundo.core.Persistence;

namespace fundo.core
{
    internal class Settings
    {
        private const string UseIndexKey = "UseIndex";
        private const string TrueValue = "true";
        private const string FalseValue = "false";

        public static bool UseIndex
        {
            get
            {
                string? value = SearchIndexStore.GetConfigValue(UseIndexKey);
                if (value == TrueValue)
                {
                    return true;
                }
                return false;
            }
            set
            {
                SearchIndexStore.SetConfigValue(UseIndexKey, value ? TrueValue : FalseValue);
            }
        }
    }
}
