using System.ComponentModel.DataAnnotations;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Leave
    {
        [Key]
        public Guid LeaveId { get; set; }
        public Guid EmployeeId { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string? LeaveStatus { get; set; }
        public string? Remarks { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
