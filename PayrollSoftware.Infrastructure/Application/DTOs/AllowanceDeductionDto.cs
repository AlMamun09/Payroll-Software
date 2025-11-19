namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class AllowanceDeductionDto
    {
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
        public string? EffectiveFromMonth { get; set; }
        public string? EffectiveToMonth { get; set; }
        
        public bool IsActive { get; set; }
        public bool IsCompanyWide { get; set; }
    }
}
