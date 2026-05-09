using System.Collections.Generic;

namespace PharmaRosterLib.Helpers.ImportSchedule
{
    /// <summary>
    /// 班表匯入模板固定列
    /// </summary>
    public class ImportScheduleTemplateRow
    {
        /// <summary>
        /// 班別類型
        /// </summary>
        public string ShiftType { get; set; }

        /// <summary>
        /// 時段
        /// </summary>
        public string ShiftTime { get; set; }
    }

    /// <summary>
    /// 班表匯入模板定義
    /// </summary>
    public static class ImportScheduleTemplateDefinition
    {
        /// <summary>
        /// 取得固定班別列
        /// </summary>
        public static List<ImportScheduleTemplateRow> GetRows()
        {
            return new List<ImportScheduleTemplateRow>
            {
                new ImportScheduleTemplateRow { ShiftType = "國定假日", ShiftTime = "08:00-12:00" },
                new ImportScheduleTemplateRow { ShiftType = "假日門診", ShiftTime = "07:30-16:00" },
                new ImportScheduleTemplateRow { ShiftType = "假日門診", ShiftTime = "08:00-16:00" },
                new ImportScheduleTemplateRow { ShiftType = "假日急診", ShiftTime = "08:00-16:00" },
                new ImportScheduleTemplateRow { ShiftType = "化療", ShiftTime = "08:00-12:00" },
                new ImportScheduleTemplateRow { ShiftType = "TPN", ShiftTime = "08:00-16:00" },
                new ImportScheduleTemplateRow { ShiftType = "中藥局", ShiftTime = "12:30-21:00" },
                new ImportScheduleTemplateRow { ShiftType = "小夜門診", ShiftTime = "12:30-21:00" },
                new ImportScheduleTemplateRow { ShiftType = "小夜門診", ShiftTime = "13:30-22:00" },
                new ImportScheduleTemplateRow { ShiftType = "小夜門診", ShiftTime = "14:30-23:00" },
                new ImportScheduleTemplateRow { ShiftType = "小夜門診", ShiftTime = "15:30-23:59" },
                new ImportScheduleTemplateRow { ShiftType = "小夜急診", ShiftTime = "16:00-23:59" },
                new ImportScheduleTemplateRow { ShiftType = "小夜其他", ShiftTime = "12:30-21:00" },
                new ImportScheduleTemplateRow { ShiftType = "大夜門診", ShiftTime = "00:00-08:00" },
                new ImportScheduleTemplateRow { ShiftType = "大夜急診", ShiftTime = "00:00-08:00" }
            };
        }
    }
}