using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace SteelTube.Desktop.Common
{
    /// <summary>
    /// SRS 15.3 -- CSV export "for business users who want to inspect data
    /// using spreadsheet software". Deliberately simple: it operates on
    /// whatever DTOs a screen has already loaded rather than re-querying,
    /// and is never used as a synchronization source (SRS 15.3 is explicit
    /// that CSV must not be used for that -- JSON via
    /// ISynchronizationSerializer is the only sync format).
    /// </summary>
    public static class CsvExporter
    {
        /// <summary>Shows a Save dialog and writes <paramref name="rows"/> as CSV if the user confirms. Returns the chosen path, or null if cancelled.</summary>
        public static string ExportWithDialog<T>(IEnumerable<T> rows, string suggestedFileName, params (string Header, Func<T, object> Value)[] columns)
        {
            var dialog = new SaveFileDialog
            {
                FileName = suggestedFileName,
                Filter = "CSV file (*.csv)|*.csv"
            };
            if (dialog.ShowDialog() != true)
                return null;

            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", columns.Select(c => Escape(c.Header))));

            foreach (var row in rows)
            {
                var values = columns.Select(c => Escape(FormatValue(c.Value(row))));
                builder.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8);
            return dialog.FileName;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return string.Empty;
            if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm");
            if (value is decimal d) return d.ToString("0.####");
            return value.ToString();
        }

        /// <summary>RFC 4180-style escaping: quote any field containing a comma, quote, or newline; double internal quotes.</summary>
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}