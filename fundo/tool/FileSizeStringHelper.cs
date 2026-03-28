using System;
using System.Globalization;

namespace fundo.tool
{
    /// <summary>
    /// Hilfsfunktionen zur Formatierung von Dateigrößen als menschenlesbare Strings
    /// mit Binärpräfixen (KiB, MiB, ...).
    /// </summary>
    internal static class FileSizeStringHelper
    {
        // Reihenfolge der Maßeinheiten, wird im Algorithmus verwendet.
        private static readonly string[] SizeUnits =
        {
            "Bytes",
            "KiB",
            "MiB",
            "GiB",
            "TiB",
            "PiB",
            "EiB",
            "ZiB",
            "YiB",
            "RiB"
        };

        /// <summary>
        /// Wandelt eine Anzahl Bytes in einen menschenlesbaren String mit Binärpräfix und Einheit um.
        /// Beispiele: "512 B", "1.23 KiB", "4.56 MiB".
        /// </summary>
        /// <param name="bytes">Anzahl Bytes (kann negativ sein).</param>
        /// <param name="decimalPlaces">Anzahl Nachkommastellen für Bruchwerte (standard: 2).</param>
        /// <returns>Formatierter String einschließlich Maßeinheit.</returns>
        public static string ToHumanReadable(long bytes, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) decimalPlaces = 0;

            // handle special case of long.MinValue
            if (bytes == long.MinValue)
            {
                // avoid overflow when taking abs
                return $"{(double)bytes:N0} B";
            }
            
            var negative = bytes < 0;
            var value = Math.Abs((double)bytes);

            // Wähle passende Einheit durch wiederholtes Teilen durch 1024.
            int unitIndex = 0;
            while (value >= 1024.0 && unitIndex < SizeUnits.Length - 1)
            {
                value /= 1024.0;
                unitIndex++;
            }

            // Bytes ohne Nachkommastellen, höhere Einheiten mit konfigurierbaren Nachkommastellen.
            string format = unitIndex == 0
                ? "N0"
                : "N" + decimalPlaces.ToString(CultureInfo.CurrentCulture);

            string sign = negative ? "-" : string.Empty;
            string number = value.ToString(format, CultureInfo.CurrentCulture);
            string unit = SizeUnits[unitIndex];

            return string.Format(CultureInfo.CurrentCulture, "{0}{1} {2}", sign, number, unit);
        }
    }
}
