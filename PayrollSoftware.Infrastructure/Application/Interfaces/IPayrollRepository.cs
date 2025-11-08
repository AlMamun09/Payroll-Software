using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IPayrollRepository
    {
        Task<List<Payroll>> GetAllPayrollsAsync();
        Task<Payroll?> GetPayrollByIdAsync(Guid payrollId);
        Task<Payroll> ProcessPayrollAsync(
            Guid employeeId,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken ct = default
        );
        Task UpdatePayrollAsync(Payroll payroll);
        Task DeletePayrollAsync(Guid payrollId);
        Task<bool> ExistsAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd);
    }
}
