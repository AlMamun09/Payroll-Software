using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Attendance
    {
        [Key]
        public Guid AttendanceId { get; set; }

        [ForeignKey("Employee")]
        public Guid EmployeeId { get; set; }

        [ForeignKey("Shift")]
        public Guid ShiftId { get; set; }

        public DateTime AttendanceDate { get; set; }
        public TimeSpan? InTime { get; set; }
        public TimeSpan? OutTime { get; set; }
        public string? Status { get; set; }
        public decimal WorkingHours { get; set; }
        public TimeSpan? LateEntry { get; set; }
        public TimeSpan? EarlyLeave { get; set; }

        // Navigation properties
        public virtual Employee? Employee { get; set; }
        public virtual Shift? Shift { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
