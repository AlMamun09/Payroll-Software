namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class AttendanceImportResultDto
    {
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
