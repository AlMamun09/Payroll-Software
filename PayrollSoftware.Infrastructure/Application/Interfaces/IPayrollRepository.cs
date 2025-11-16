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
        Task UpdatePayrollAsync(Payroll payroll, string? updatedBy = null);
        Task<bool> ExistsAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd);
    }
}
