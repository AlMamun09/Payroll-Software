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
        Task AddAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction);
        Task UpdateAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction);
        Task DeleteAllowanceDeductionAsync(Guid allowanceDeductionId);
        Task ValidateAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction);
    }
}
