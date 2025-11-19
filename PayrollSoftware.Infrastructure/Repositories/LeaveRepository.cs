using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly ApplicationDbContext _context;

        // Validation constants
        private readonly string[] _validLeaveTypes = { "Casual", "Sick", "Earned", "Unpaid" };
        private readonly string[] _validLeaveStatuses = { "Pending", "Approved", "Rejected" };
        private const int RemarksMaxLength = 500;
        private const int MaxLeaveDurationDays = 90;
        private const int MinAdvanceNoticeDays = 1;
        private const int MaxFutureLeaveDays = 365;

        // Regex patterns
        private readonly Regex _remarksRegex = new Regex(
            @"^[a-zA-Z0-9\s\.,!?\-_()@#$%&*+=:;'""]*$",
            RegexOptions.Compiled
        );

        public LeaveRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Leave>> GetAllLeavesAsync()
        {
            try
            {
                return await _context.Leaves.OrderByDescending(l => l.StartDate).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving all leaves.", ex);
            }
        }

        public async Task<List<Leave>> GetLeavesByEmployeeIdAsync(Guid employeeId)
        {
            await ValidateEmployeeExistsAsync(employeeId);
            try
            {
                return await _context
                    .Leaves.Where(l => l.EmployeeId == employeeId)
                    .OrderByDescending(l => l.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occurred while retrieving leaves for employee {employeeId}.",
                    ex
                );
            }
        }

        public async Task<List<Leave>> GetLeavesByStatusAsync(string leaveStatus)
        {
            ValidateLeaveStatus(leaveStatus);
            try
            {
                return await _context
                    .Leaves.Where(l => l.LeaveStatus == leaveStatus)
                    .OrderByDescending(l => l.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occurred while retrieving {leaveStatus} leaves.",
                    ex
                );
            }
        }

        public async Task<Leave?> GetLeaveByIdAsync(Guid leaveId)
        {
            if (leaveId == Guid.Empty)
                throw new ArgumentException("Leave ID cannot be empty.");

            try
            {
                return await _context.Leaves.FirstOrDefaultAsync(l => l.LeaveId == leaveId);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while retrieving leave {leaveId}.", ex);
            }
        }

        public async Task<Leave> ApplyForLeaveAsync(Leave leave)
        {
            await ValidateLeaveApplicationAsync(leave);

            try
            {
                if (string.IsNullOrWhiteSpace(leave.LeaveStatus))
                    leave.LeaveStatus = "Pending";

                leave.Remarks = SanitizeRemarks(leave.Remarks);

                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();
                return leave;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("An error occurred while saving the leave application.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while applying for leave.", ex);
            }
        }

        // [NEW] Method to update full details (Edit Mode)
        public async Task<Leave> UpdateLeaveDetailsAsync(Leave leave)
        {
            if (leave == null)
                throw new ArgumentNullException(nameof(leave));

            var existingLeave = await GetLeaveByIdAsync(leave.LeaveId);
            if (existingLeave == null)
                throw new ArgumentException($"Leave with ID {leave.LeaveId} not found.");

            // Only allow editing if the leave is still Pending
            if (existingLeave.LeaveStatus != "Pending")
            {
                throw new InvalidOperationException(
                    "Only 'Pending' leaves can be edited. Please cancel and apply for a new leave."
                );
            }

            // Basic validation
            await ValidateEmployeeExistsAsync(leave.EmployeeId);

            // Check for overlaps (excluding self)
            if (
                await HasLeaveOverlapAsync(
                    leave.EmployeeId,
                    leave.StartDate,
                    leave.EndDate,
                    leave.LeaveId
                )
            )
            {
                throw new ArgumentException(
                    "The updated dates overlap with another existing leave."
                );
            }

            try
            {
                // Update properties
                existingLeave.EmployeeId = leave.EmployeeId;
                existingLeave.LeaveType = leave.LeaveType;
                existingLeave.StartDate = leave.StartDate;
                existingLeave.EndDate = leave.EndDate;
                existingLeave.TotalDays = leave.TotalDays;
                existingLeave.Remarks = SanitizeRemarks(leave.Remarks);

                _context.Leaves.Update(existingLeave);
                await _context.SaveChangesAsync();

                return existingLeave;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("An error occurred while updating the leave details.", ex);
            }
        }

        // [RESTORED] Method to update just status (Approve/Reject)
        public async Task<Leave> UpdateLeaveStatusAsync(Guid leaveId, string status, string remarks)
        {
            ValidateLeaveStatus(status);

            if (leaveId == Guid.Empty)
                throw new ArgumentException("Leave ID cannot be empty.");

            var leave = await GetLeaveByIdAsync(leaveId);
            if (leave == null)
                throw new ArgumentException($"Leave with ID {leaveId} not found.");

            ValidateStatusTransition(leave.LeaveStatus, status);

            try
            {
                leave.LeaveStatus = status;
                leave.Remarks = SanitizeRemarks(remarks);

                _context.Leaves.Update(leave);
                await _context.SaveChangesAsync();

                return leave;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("An error occurred while updating the leave status.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating leave status.", ex);
            }
        }

        public async Task DeleteLeaveAsync(Guid leaveId)
        {
            if (leaveId == Guid.Empty)
                throw new ArgumentException("Leave ID cannot be empty.");

            var leave = await GetLeaveByIdAsync(leaveId);
            if (leave == null)
                throw new ArgumentException($"Leave with ID {leaveId} not found.");

            if (leave.LeaveStatus == "Approved")
            {
                throw new InvalidOperationException(
                    "Cannot delete an approved leave. Please reject it first."
                );
            }

            try
            {
                _context.Leaves.Remove(leave);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("An error occurred while deleting the leave.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while deleting leave.", ex);
            }
        }

        public async Task<List<Leave>> GetLeavesByDateRangeAsync(
            DateTime startDate,
            DateTime endDate
        )
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date cannot be after end date.");

            try
            {
                return await _context
                    .Leaves.Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                    .OrderByDescending(l => l.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving leaves by date range.", ex);
            }
        }

        public async Task<List<Leave>> GetPendingLeavesAsync()
        {
            return await GetLeavesByStatusAsync("Pending");
        }

        public async Task<bool> HasLeaveOverlapAsync(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeLeaveId = null
        )
        {
            if (employeeId == Guid.Empty)
                throw new ArgumentException("Employee ID cannot be empty.");

            await ValidateEmployeeExistsAsync(employeeId);

            try
            {
                var query = _context.Leaves.Where(l =>
                    l.EmployeeId == employeeId
                    && l.LeaveStatus != "Rejected"
                    && l.StartDate <= endDate
                    && l.EndDate >= startDate
                );

                if (excludeLeaveId.HasValue && excludeLeaveId.Value != Guid.Empty)
                {
                    query = query.Where(l => l.LeaveId != excludeLeaveId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking for leave overlaps.", ex);
            }
        }

        private async Task ValidateLeaveApplicationAsync(Leave leave)
        {
            if (leave == null)
                throw new ArgumentNullException(nameof(leave));

            var errors = new List<string>();

            if (leave.EmployeeId == Guid.Empty)
                errors.Add("Employee ID is required.");
            if (string.IsNullOrWhiteSpace(leave.LeaveType))
                errors.Add("Leave type is required.");

            // Basic date checks
            if (leave.StartDate < DateTime.Today)
                errors.Add("Start date cannot be in the past.");
            if (leave.EndDate < leave.StartDate)
                errors.Add("End date cannot be before start date.");

            // Check employee exists
            if (leave.EmployeeId != Guid.Empty)
            {
                var employeeExists = await _context
                    .Employees.AsNoTracking()
                    .AnyAsync(e => e.EmployeeId == leave.EmployeeId);
                if (!employeeExists)
                    errors.Add($"Employee with ID {leave.EmployeeId} does not exist.");
            }

            // Overlap check
            if (
                leave.EmployeeId != Guid.Empty
                && leave.StartDate >= DateTime.Today
                && leave.EndDate >= leave.StartDate
            )
            {
                if (await HasLeaveOverlapAsync(leave.EmployeeId, leave.StartDate, leave.EndDate))
                    errors.Add(
                        "Employee already has an approved or pending leave application for this date range."
                    );
            }

            if (errors.Any())
                throw new ArgumentException(string.Join("\n", errors));
        }

        private void ValidateLeaveStatus(string leaveStatus)
        {
            if (string.IsNullOrWhiteSpace(leaveStatus))
                throw new ArgumentException("Leave status is required.");

            if (!_validLeaveStatuses.Contains(leaveStatus))
                throw new ArgumentException($"Invalid leave status '{leaveStatus}'.");
        }

        private void ValidateStatusTransition(string currentStatus, string newStatus)
        {
            var allowedTransitions = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Approved", "Rejected" } },
                { "Approved", new[] { "Rejected" } },
                { "Rejected", new[] { "Approved" } },
            };

            if (
                allowedTransitions.ContainsKey(currentStatus)
                && !allowedTransitions[currentStatus].Contains(newStatus)
            )
            {
                throw new InvalidOperationException(
                    $"Cannot change status from '{currentStatus}' to '{newStatus}'."
                );
            }
        }

        private string SanitizeRemarks(string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
                return null;
            remarks = remarks.Trim();
            remarks = Regex.Replace(remarks, @"\s+", " ");
            if (remarks.Length > RemarksMaxLength)
                remarks = remarks.Substring(0, RemarksMaxLength);
            return remarks;
        }

        private async Task ValidateEmployeeExistsAsync(Guid employeeId)
        {
            var exists = await _context
                .Employees.AsNoTracking()
                .AnyAsync(e => e.EmployeeId == employeeId);
            if (!exists)
                throw new ArgumentException($"Employee with ID {employeeId} does not exist.");
        }
    }
}
