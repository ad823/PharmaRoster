using System.Collections.Generic;

public class PreviewImportScheduleExcelResponse
{
    public string file_name { get; set; }
    public string total_cells { get; set; }
    public string success_cells { get; set; }
    public string error_cells { get; set; }
    public List<PreviewImportScheduleExcelCellResult> results { get; set; } = new List<PreviewImportScheduleExcelCellResult>();
}

public class PreviewImportScheduleExcelCellResult
{
    public string row_index { get; set; }
    public string column_index { get; set; }
    public string date_text { get; set; }
    public string shift_type { get; set; }
    public string shift_time { get; set; }
    public string raw_text { get; set; }
    public string parsed_simple_names { get; set; }
    public string parsed_staff_ids { get; set; }
    public string parsed_staff_names { get; set; }
    public string is_success { get; set; }
    public string error_message { get; set; }
}