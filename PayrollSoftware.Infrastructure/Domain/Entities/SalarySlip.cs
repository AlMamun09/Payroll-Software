using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class SalarySlip
    {
        [Key]
        public Guid SalarySlipId { get; set; }

        [ForeignKey("Payroll")]
        public Guid PayrollId { get; set; }

        [ForeignKey("Employee")]
        public Guid EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedDate { get; set; }

        // Navigation properties
        public virtual Payroll? Payroll { get; set; }
        public virtual Employee? Employee { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
