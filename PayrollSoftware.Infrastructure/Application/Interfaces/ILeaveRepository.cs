using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface ILeaveRepository
    {
        Task<List<Leave>> GetAllLeavesAsync();
        Task<List<Leave>> GetLeavesByEmployeeIdAsync(Guid employeeId);
        Task<List<Leave>> GetLeavesByStatusAsync(string leaveStatus);
        Task<Leave?> GetLeaveByIdAsync(Guid leaveId);
        Task<Leave> ApplyForLeaveAsync(Leave leave);
        Task<Leave> UpdateLeaveStatusAsync(Guid leaveId, string status, string remarks);
        Task DeleteLeaveAsync(Guid leaveId);
        Task<List<Leave>> GetLeavesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Leave>> GetPendingLeavesAsync();
        Task<bool> HasLeaveOverlapAsync(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeLeaveId = null
        );
    }
}
