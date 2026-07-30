namespace Sheet2Schedule.Models
{
    /// <summary>One detected problem with a specific Excel row, found before import.</summary>
    public class ValidationIssue
    {
        public int RowNumber { get; set; }       // 1-based, matches the row's position in the data
        public string Message { get; set; }
    }
}