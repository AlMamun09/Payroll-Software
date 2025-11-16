using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IAllowanceDeductionRepository
    {
        Task<List<AllowanceDeduction>> GetAllAllowanceDeductionsAsync();
        Task<List<AllowanceDeduction>> GetAllActiveAllowanceDeductionsAsync();
        Task<List<AllowanceDeduction>> GetCompanyWideAllowanceDeductionsAsync();
        Task<List<AllowanceDeduction>> GetEmployeeSpecificAllowanceDeductionsAsync(Guid employeeId);
        Task<AllowanceDeduction?> GetAllowanceDeductionByIdAsync(Guid allowanceDeductionId);
        Task AddAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction, string? createdBy = null);
        Task UpdateAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction, string? updatedBy = null);
        Task DeactivateAllowanceDeductionAsync(Guid allowanceDeductionId, string? updatedBy = null);
        Task ActivateAllowanceDeductionAsync(Guid allowanceDeductionId, string? updatedBy = null);
        Task ValidateAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction);
    }
}
