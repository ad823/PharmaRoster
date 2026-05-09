using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PharmaRosterLib.Helpers.ImportSchedule
{
    /// <summary>
    /// 班表匯入 Excel 相關工具
    /// </summary>
    public static class ImportScheduleExcelHelper
    {
        /// <summary>
        /// 取得儲存格字串內容
        /// </summary>
        public static string GetCellString(ICell cell)
        {
            if (cell == null) return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue?.Trim() ?? string.Empty;

                case CellType.Numeric:
                    return cell.NumericCellValue.ToString().Trim();

                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString().Trim();

                case CellType.Formula:
                    try
                    {
                        return cell.ToString().Trim();
                    }
                    catch
                    {
                        return string.Empty;
                    }

                case CellType.Blank:
                    return string.Empty;

                default:
                    return cell.ToString().Trim();
            }
        }

        /// <summary>
        /// 驗證是否為合法日期欄文字（01~31）
        /// </summary>
        public static bool IsValidDayHeader(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!int.TryParse(text.Trim(), out int day)) return false;
            return day >= 1 && day <= 31;
        }

        /// <summary>
        /// 將儲存格內容拆為簡名清單（規則：一字一人）
        /// </summary>
        public static List<string> ParseSimpleNames(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return new List<string>();

            return rawText.Trim()
                          .Select(c => c.ToString())
                          .ToList();
        }

        /// <summary>
        /// 檢查內容是否包含不允許字元
        /// </summary>
        public static bool ContainsInvalidCharacters(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return false;

            return rawText.Contains(" ")
                || rawText.Contains("　")
                || rawText.Contains("、")
                || rawText.Contains("[")
                || rawText.Contains("]")
                || rawText.Contains(",")
                || rawText.Contains("，")
                || rawText.Contains("\r")
                || rawText.Contains("\n")
                || rawText.Contains("\t");
        }

        /// <summary>
        /// 找出重複簡名
        /// </summary>
        public static List<string> GetDuplicatedSimpleNames(List<string> simpleNames)
        {
            if (simpleNames == null) return new List<string>();

            return simpleNames
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
        }

        /// <summary>
        /// 建立日期欄對照表
        /// key = 欄位 index
        /// value = 日期文字（01~31）
        /// </summary>
        public static Dictionary<int, string> BuildDateColumnMap(IRow headerRow, int startCol = 2, int endCol = 32)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            if (headerRow == null) return result;

            for (int col = startCol; col <= endCol; col++)
            {
                string text = GetCellString(headerRow.GetCell(col));
                if (IsValidDayHeader(text))
                {
                    result[col] = text.Trim();
                }
            }

            return result;
        }

        /// <summary>
        /// 比對固定班別列是否正確
        /// </summary>
        public static bool IsTemplateRowMatched(string actualShiftType, string actualShiftTime, ImportScheduleTemplateRow expectedRow)
        {
            if (expectedRow == null) return false;

            return string.Equals(actualShiftType?.Trim(), expectedRow.ShiftType?.Trim(), StringComparison.Ordinal)
                && string.Equals(actualShiftTime?.Trim(), expectedRow.ShiftTime?.Trim(), StringComparison.Ordinal);
        }
    }
}