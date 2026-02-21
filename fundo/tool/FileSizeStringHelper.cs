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
        /// <summary>
        /// Wandelt eine Anzahl Bytes in einen menschenlesbaren String mit Binärpräfix und Einheit um.
        /// Beispiele: "512 B", "1.23 KiB", "4.56 MiB".
        /// </summary>
        /// <param name="bytes">Anzahl Bytes (kann negativ sein).</param>
        /// <param name="decimalPlaces">Anzahl Nachkommastellen für Bruchwerte (standard: 2).</param>
        /// <returns>Formatierter String einschließlich Maßeinheit.</returns>
        public static string ToHumanReadable(long bytes, int decimalPlaces = 2)
        {
            const long KIB = 1024L;
            const long MIB = KIB * 1024L;
            const long GIB = MIB * 1024L;
            const long TIB = GIB * 1024L;
            const long PIB = TIB * 1024L;
            const long EIB = PIB * 1024L;

            if (decimalPlaces < 0) decimalPlaces = 0;

            // handle special case of long.MinValue
            if (bytes == long.MinValue)
            {
                // avoid overflow when taking abs
                return $"{(double)bytes:N0} B";
            }

            var negative = bytes < 0;
            var absBytes = Math.Abs((double)bytes);

            string format = "N" + decimalPlaces.ToString(CultureInfo.CurrentCulture);

            if (absBytes < KIB)
            {
                // show as integer bytes
                var value = (long)absBytes;
                return string.Format(CultureInfo.CurrentCulture, "{0}{1} Bytes", negative ? "-" : "", value);
            }

            if (absBytes < MIB)
            {
                var v = absBytes / KIB;
                return string.Format(CultureInfo.CurrentCulture, "{0}{1} KiB", negative ? "-" : "", v.ToString(format, CultureInfo.CurrentCulture));
            }

            if (absBytes < GIB)
            {
                var v = absBytes / MIB;
                return string.Format(CultureInfo.CurrentCulture, "{0}{1} MiB", negative ? "-" : "", v.ToString(format, CultureInfo.CurrentCulture));
            }

            if (absBytes < TIB)
            {
                var v = absBytes / GIB;
                return string.Format(CultureInfo.CurrentCulture, "{0}{1} GiB", negative ? "-" : "", v.ToString(format, CultureInfo.CurrentCulture));
            }

            if (absBytes < PIB)
            {
                var v = absBytes / TIB;
                return string.Format(CultureInfo.CurrentCulture, "{0}{1} TiB", negative ? "-" : "", v.ToString(format, CultureInfo.CurrentCulture));
            }

            if (absBytes < EIB)
            {
                var v = absBytes / PIB;
                return string.Format(CultureInfo.CurrentCulture, "{0}{1} PiB", negative ? "-" : "", v.ToString(format, CultureInfo.CurrentCulture));
            }

            // exbibyte or larger
            var ve = absBytes / EIB;
            return string.Format(CultureInfo.CurrentCulture, "{0}{1} EiB", negative ? "-" : "", ve.ToString(format, CultureInfo.CurrentCulture));
        }
    }
}
