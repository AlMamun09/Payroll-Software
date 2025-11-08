using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface ISalarySlipRepository
    {
        Task<SalarySlip?> GetSalarySlipByIdAsync(Guid salarySlipId);
        Task<SalarySlip?> GetSalarySlipByPayrollIdAsync(Guid payrollId);
        Task<List<SalarySlip>> GetAllSalarySlipsAsync();
        Task<List<SalarySlip>> GetSalarySlipsByEmployeeIdAsync(Guid employeeId);
        Task<SalarySlip> CreateSalarySlipAsync(SalarySlip salarySlip);
        Task UpdateSalarySlipAsync(SalarySlip salarySlip);
        Task DeleteSalarySlipAsync(Guid salarySlipId);
    }
}
