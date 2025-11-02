

using NPOI.SS.UserModel;
using System.Data;

namespace PharmaRosterAPI
{
    static public class Function
    {
        public static DataTable SheetToDataTable(this ISheet sheet, bool firstRowIsHeader = true)
        {
            DataTable table = new DataTable();

            if (sheet == null) return table;

            IRow firstRow = sheet.GetRow(0);
            int cellCount = firstRow.LastCellNum;

            // 產生欄位名稱
            for (int i = 0; i < cellCount; i++)
            {
                string columnName = firstRowIsHeader
                    ? firstRow.GetCell(i)?.ToString().Trim()
                    : $"Column{i}";
                if (string.IsNullOrWhiteSpace(columnName)) columnName = $"Column{i}";
                table.Columns.Add(columnName);
            }

            int startRow = firstRowIsHeader ? 1 : 0;

            // 逐列讀取資料
            for (int rowIndex = startRow; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                DataRow dataRow = table.NewRow();

                for (int colIndex = 0; colIndex < cellCount; colIndex++)
                {
                    dataRow[colIndex] = row.GetCell(colIndex)?.ToString().Trim();
                }

                table.Rows.Add(dataRow);
            }

            return table;
        }
    }
}
