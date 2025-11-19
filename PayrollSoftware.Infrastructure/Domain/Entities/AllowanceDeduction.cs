using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class AllowanceDeduction
    {
        [Key]
        public Guid AllowanceDeductionId { get; set; }

        [ForeignKey("Payroll")]
        public Guid? PayrollId { get; set; }

        [ForeignKey("Employee")]
        public Guid? EmployeeId { get; set; }

        public string? AllowanceDeductionType { get; set; }
        public string? AllowanceDeductionName { get; set; }
        public string? CalculationType { get; set; }
        public decimal Percentage { get; set; }
        public decimal FixedAmount { get; set; }
        
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
   
        public bool IsActive { get; set; } = true;
        public bool IsCompanyWide { get; set; }

        public virtual Payroll? Payroll { get; set; }
        public virtual Employee? Employee { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
