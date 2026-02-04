using System;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.Extensions
{
    public static class OtherExtensions
    {

        public static void DebugLogTable(this DataTable table, string title = "DataTable", int maxRows = int.MaxValue)
        {
            if (table == null)
            {
                Debug.Log(SharedLocKey.log_null_datatable.Localize());
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{title}] Rows={table.Rows.Count}, Columns={table.Columns.Count}");

            // Header
            string[] headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            sb.AppendLine(string.Join(" | ", headers));
            sb.AppendLine(new string('-', Math.Max(3, headers.Sum(h => h.Length) + (headers.Length - 1) * 3)));

            // Rows
            int rowCount = Math.Min(table.Rows.Count, maxRows);
            for (int r = 0; r < rowCount; r++)
            {
                DataRow row = table.Rows[r];
                var cells = table.Columns.Cast<DataColumn>()
                    .Select(c => FormatCell(row[c]));
                sb.AppendLine(string.Join(" | ", cells));
            }

            if (table.Rows.Count > rowCount)
                sb.AppendLine($"... {table.Rows.Count - rowCount} more rows");

            Debug.LogError(sb.ToString());
        }

        private static string FormatCell(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            // Make newlines visible in logs
            return value.ToString().Replace("\r", "\\r").Replace("\n", "\\n");
        }


        public static bool IsDefault<T>(this T obj)
        {
            if (obj == null)
            {
                return true;
            }

            return obj.Equals(default(T));
        }

        public static bool IsFlagSet<T>(this T value, T flag) where T : struct
        {
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flag);
            return (lValue & lFlag) != 0;
        }

        public static T CopyClass<T>(this T source)
        {
            string json = JsonUtility.ToJson(source);
            T copiedObject = JsonUtility.FromJson<T>(json);
            return copiedObject;
        }

        /// <summary>
        /// <para><see href="https://stackoverflow.com/a/33784596/8265642"/></para>
        /// </summary>
        public static string GetNumbers(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return new string(text.Where(p => char.IsDigit(p)).ToArray());
        }

        public static bool ToBoolNullTrue(this bool? value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Value;
        }

        public static bool ToBoolNullFalse(this bool? value)
        {
            if (value == null)
            {
                return false;
            }

            return value.Value;
        }
    }
}
