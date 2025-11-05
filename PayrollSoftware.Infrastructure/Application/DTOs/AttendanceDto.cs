namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class AttendanceDto
    {
        public Guid AttendanceId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ShiftId { get; set; }
        public Guid LeaveId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? InTime { get; set; }
        public TimeSpan? OutTime { get; set; }
        public string? Status { get; set; }
        public decimal WorkingHours { get; set; }
        public TimeSpan? LateEntry { get; set; }
        public TimeSpan? EarlyLeave { get; set; }
    }
}
