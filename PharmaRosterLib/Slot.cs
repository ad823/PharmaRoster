using Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaRosterLib
{
    public class NightSlot
    {
        public DateTime Date { get; set; }          // 該班別的日曆日期（起算日）
        public string Time { get; set; }            // 原始字串(例如 "16:00-00:00")
        public int RequiredCount { get; set; }      // 攤平後固定為 1
        public int AssignedCount { get; set; }      // 已指派人數（0 或 1）
        public RequiredShiftClass Req { get; set; } // 對應的需求物件
        public WorkShiftRequirementClass Wr { get; set; } // 對應的時段需求
        public bool IsBase { get; set; }            // 是否為「最後班」（15:30/16:00 收 24:00/00:00/23:59）
        public DateTime Start { get; set; }         // 實際開始時間（含日期）
        public DateTime End { get; set; }           // 實際結束時間（含日期；必要時 +1 天，處理跨日）


        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} {Time} (Req: {RequiredCount}, Assigned: {AssignedCount})";
        }
    }
    public static class NightSlotHelper
    {
        /// <summary>
        /// 依起始日期與連續天數，自動產生每日班別（由晚到早）
        /// </summary>
        /// <param name="dateStart">起始日期</param>
        /// <param name="days">連續天數</param>
        /// <returns>List&lt;(string Date, string ShiftTime)&gt; 日期+班別組合</returns>
        public static List<(string Date, string ShiftTime)> GenerateSequentialShiftsSimple(
            DateTime dateStart,
            int days)
        {
            // 固定班別順序：由晚到早
            var shifts = new List<string>
            {
                "16:00–23:59",
                "15:30–23:59",
                "14:30–23:00",
                "13:30–22:00",
                "12:30–21:00"
            };

            var result = new List<(string Date, string ShiftTime)>();

            for (int i = 0; i < days; i++)
            {
                var date = dateStart.AddDays(i).ToString("yyyy-MM-dd");
                // 若天數超過五天，從最早班(12:30–21:00)開始重複
                var shift = shifts[Math.Min(i, shifts.Count - 1)];
                result.Add((date, shift));
            }

            return result;
        }

        /// <summary>
        /// 從 slot 清單中導出唯一日期字串陣列（格式：yyyy-MM-dd）
        /// </summary>
        public static List<string> GetDateStringsFromSlots(this List<NightSlot> slots)
        {
            if (slots == null || slots.Count == 0) return new List<string>();

            return slots
                .Select(s => s.Date.Date.ToString("yyyy-MM-dd"))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
        /// <summary>
        /// 依照班別分組且排序
        /// </summary>
        public static Dictionary<string, List<NightSlot>> GroupAndSortByShiftType(this List<NightSlot> slots)
        {
            if (slots == null) return new Dictionary<string, List<NightSlot>>();

            // 先依 shift_group_guid 分組
            var groups = slots
                .GroupBy(s => s.Req?.shift_group_guid ?? "")
                .ToDictionary(g => g.Key, g =>
                    g.OrderBy(s => s.Date)
                     .ThenBy(s => GetShiftPriority(s.Start.ToString("HH:mm")))
                     .ToList());

            return groups;
        }

        /// <summary>
        /// 輸入日期與起訖時間找出有需求的班次 (尚未滿員)
        /// </summary>
        public static List<NightSlot> FindSlotByDate(this List<NightSlot> slots, DateTime date)
        {


            return slots
                .Where(s =>
                    s.Date.Date == date.Date &&
                    s.AssignedCount < s.RequiredCount)
                .ToList();
        }



        /// <summary>
        /// 輸入多個日期與時段，找出符合的班次 (尚未滿員)
        /// 若任一組合無匹配，則回傳 Count = 0
        /// </summary>
        public static List<NightSlot> FindSlotsByDatesAndTimeRanges( this List<NightSlot> slots, List<DateTime> dates, List<string> timeRanges)
        {
            var result = new List<NightSlot>();
            if (slots == null || dates == null || timeRanges == null) return result;

            // 正規化所有時間字串
            var normalizedTimes = timeRanges
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => NormalizeTimeString(t))
                .Distinct()
                .ToList();

            // 若沒有有效時間字串
            if (normalizedTimes.Count == 0) return result;

            // 用於記錄是否有找不到的組合
            bool anyMissing = false;

            for(int d = 0; d < dates.Count; d++)
            {
                List<NightSlot> found = slots
                    .Where(s =>
                        s.Date.Date == dates[d].Date &&
                        s.AssignedCount < s.RequiredCount &&
                        NormalizeTimeString(s.Time) == timeRanges[d])
                    .ToList();
                if(found.Count == 0)
                {
                    anyMissing = true;
                    break;
                }
                result.Add(found[0]);
            }
          
            // 若有任一組合沒找到 → 清空結果
            if (anyMissing) result.Clear();

            return result;
        }

        /// <summary>
        /// 輸入日期與起訖時間找出有需求的班次 (尚未滿員)
        /// </summary>
        public static List<NightSlot> FindSlotByDateTimeRange(this List<NightSlot> slots, DateTime date,string timeRange)
        {
            if (slots == null || string.IsNullOrWhiteSpace(timeRange)) return new List<NightSlot>();

            string normalized = NormalizeTimeString(timeRange);

            return slots
                .Where(s =>
                    s.Date.Date == date.Date &&
                    NormalizeTimeString(s.Time) == normalized &&
                    s.AssignedCount < s.RequiredCount)
                .ToList();
        }

        /// <summary>
        /// 設定指定日期與時間的班次為完成
        /// </summary>
        public static void MarkSlotCompleted(this List<NightSlot> slots, DateTime date, string timeRange)
        {
            if (slots == null || string.IsNullOrWhiteSpace(timeRange)) return;

            string normalized = NormalizeTimeString(timeRange);

            foreach (var s in slots.Where(s =>
                         s.Date.Date == date.Date &&
                         NormalizeTimeString(s.Time) == normalized))
            {
                s.AssignedCount = s.RequiredCount;
                s.Wr.assigned_count = s.RequiredCount.ToString();
            }
        }

        /// <summary>
        /// 取得下一個仍有需求的班次 (依日期與時間順序)
        /// </summary>
        public static NightSlot FindNextAvailableSlot(this List<NightSlot> slots, DateTime currentDate, string currentTime)
        {
            if (slots == null || string.IsNullOrWhiteSpace(currentTime)) return null;

            // 排序邏輯：日期 → 班別優先權
            var ordered = slots
                .OrderBy(s => s.Date)
                .ThenBy(s => GetShiftPriority(s.Start.ToString("HH:mm")))
                .ToList();

            string normalized = NormalizeTimeString(currentTime);
            var currentIndex = ordered.FindIndex(s =>
                s.Date.Date == currentDate.Date &&
                NormalizeTimeString(s.Time) == normalized);

            if (currentIndex < 0 || currentIndex >= ordered.Count - 1)
                return null;

            // 從下一個往後找第一個仍有需求的班次
            for (int i = currentIndex + 1; i < ordered.Count; i++)
            {
                var slot = ordered[i];
                if (slot.AssignedCount < slot.RequiredCount)
                    return slot;
            }

            return null;
        }




        private static int GetShiftPriority(string hhmm)
        {
            switch (hhmm)
            {
                case "15:30": return 0;
                case "16:00": return 1;
                case "14:30": return 2;
                case "13:30": return 3;
                case "12:30": return 4;
                default: return 99;
            }
        }

        private static string NormalizeTimeString(string timeRange)
        {
            return timeRange.Replace(" ", "").Replace("：", ":").Trim();
        }
    }

    /// <summary>
    /// 依指定起始日期與天數，從 baseSlots 中找出所有可連續排班的可能組合
    /// </summary>
    public static class SlotCombinationFinder
    {
        /// <summary>
        /// 將攤平的 baseSlots 合併為每日唯一班別清單
        /// </summary>
        public static List<NightSlot> MergeDailySlots(this List<NightSlot> baseSlots)
        {
            if (baseSlots == null || baseSlots.Count == 0)
                return new List<NightSlot>();

            return baseSlots
                .GroupBy(s => new { s.Date.Date, s.Time }) // 同日同班別合併
                .Select(g =>
                {
                    var first = g.First();
                    return new NightSlot
                    {
                        Date = first.Date.Date,
                        Time = first.Time,
                        Start = first.Start,
                        End = first.End,
                        IsBase = first.IsBase,
                        Req = first.Req,
                        Wr = first.Wr,
                        RequiredCount = g.Sum(x => x.RequiredCount),
                        AssignedCount = g.Sum(x => x.AssignedCount)
                    };
                })
                .ToList();
        }

        /// <summary>
        /// 找出所有可能的連續班組合
        /// </summary>
        /// <param name="baseSlots">所有班次資料</param>
        /// <param name="startDate">起始日期</param>
        /// <param name="days">連續天數</param>
        /// <returns>可能的班別組合列表</returns>
        public static List<List<NightSlot>> FindAvailableCombinations(
            this List<NightSlot> BaseSlots,
            DateTime startDate,
            int days)
        {
            var baseSlots = BaseSlots.MergeDailySlots();
            var results = new List<List<NightSlot>>();
            if (baseSlots == null || baseSlots.Count == 0) return results;

            // === 依日期分組 ===
            var groupedByDate = baseSlots
                .GroupBy(s => s.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // === 建立連續日期清單 ===
            var dateList = Enumerable.Range(0, days)
                                     .Select(i => startDate.Date.AddDays(i))
                                     .ToList();

            // === 驗證每一天都有班可排 ===
            if (dateList.Any(d => !groupedByDate.ContainsKey(d)))
                return results; // 有日期沒有班，直接回傳空集合

            // === 遞迴組合搜尋 ===
            void Build(int depth, List<NightSlot> current)
            {
                if (depth == days)
                {
                    results.Add(new List<NightSlot>(current));
                    return;
                }

                var date = dateList[depth];
                var slots = groupedByDate[date]
                    .Where(s => s.AssignedCount < s.RequiredCount) // 尚未滿員
                    .OrderBy(s => s.Start) // 晚→早
                    .ToList();

                foreach (var slot in slots)
                {
                    // 避免同一天重複
                    if (current.Any(c => c.Date.Date == slot.Date.Date)) continue;

                    // 若前一天有班，需判斷是否連續（跨天銜接平滑）
                    if (current.Count > 0)
                    {
                        var prev = current.Last();
                        if (!IsReasonablySequential(prev, slot))
                            continue;
                    }

                    current.Add(slot);
                    Build(depth + 1, current);
                    current.RemoveAt(current.Count - 1);
                }
            }

            Build(0, new List<NightSlot>());
            return results;
        }

        /// <summary>
        /// 判斷兩班是否為合理連續（晚→早）
        /// </summary>
        private static bool IsReasonablySequential(NightSlot prev, NightSlot next)
        {
            // 同一人不該有重疊
            if (next.Start < prev.End) return false;

            // 允許由晚到早遞減，例如 16:00→15:30→14:30→13:30→12:30
            var diffHours = (next.Start - prev.End).TotalHours;
            return diffHours >= 0 && diffHours <= 16; // 不隔太久也不重疊
        }
    }


}
