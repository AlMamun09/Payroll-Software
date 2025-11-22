using System.ComponentModel.DataAnnotations;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Payroll
    {
        [Key]
        public Guid PayrollId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int PaidLeaveDays { get; set; }
        public int UnpaidLeaveDays { get; set; }
        public int AbsentDays { get; set; }
        public int PayableDays { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
