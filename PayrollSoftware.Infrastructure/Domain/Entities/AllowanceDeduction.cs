using System.ComponentModel.DataAnnotations;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class AllowanceDeduction
    {
        [Key]
        public Guid AllowanceDeductionId { get; set; }
        public Guid? PayrollId { get; set; }
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

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
