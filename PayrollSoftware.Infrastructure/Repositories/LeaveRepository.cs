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

        // Configuration constants
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
                // TotalDays is now calculated on the client side and passed from the form
                // Don't recalculate it here to maintain consistency with user input

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

            if (endDate > DateTime.Today.AddYears(1))
                throw new ArgumentException("Cannot search beyond 1 year from today.");

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

                if (excludeLeaveId.HasValue)
                {
                    if (excludeLeaveId.Value == Guid.Empty)
                        throw new ArgumentException("Exclude Leave ID cannot be empty.");

                    query = query.Where(l => l.LeaveId != excludeLeaveId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking for leave overlaps.", ex);
            }
        }

        #region Private Validation Methods

        private async Task ValidateLeaveApplicationAsync(Leave leave)
        {
            if (leave == null)
                throw new ArgumentNullException(nameof(leave));

            // Collect all validation errors
            var errors = new List<string>();

            if (leave.EmployeeId == Guid.Empty)
                errors.Add("Employee ID is required.");

            if (string.IsNullOrWhiteSpace(leave.LeaveType))
                errors.Add("Leave type is required.");
            else if (!_validLeaveTypes.Contains(leave.LeaveType))
                errors.Add(
                    $"Invalid leave type '{leave.LeaveType}'. Valid types: {string.Join(", ", _validLeaveTypes)}"
                );

            // Validate dates
            if (leave.StartDate < DateTime.Today)
                errors.Add("Start date cannot be in the past.");
            else if (leave.StartDate > DateTime.Today.AddDays(MaxFutureLeaveDays))
                errors.Add(
                    $"Leave cannot be applied more than {MaxFutureLeaveDays} days in advance."
                );

            if (leave.EndDate < leave.StartDate)
                errors.Add("End date cannot be before start date.");
            else
            {
                var maxEndDate = leave.StartDate.AddDays(MaxLeaveDurationDays * 2);
                if (leave.EndDate > maxEndDate)
                    errors.Add(
                        $"Leave duration is too long. Maximum allowed duration is {MaxLeaveDurationDays} working days."
                    );
            }

            // Check employee exists
            if (leave.EmployeeId != Guid.Empty)
            {
                var employeeExists = await _context
                    .Employees.AsNoTracking()
                    .AnyAsync(e => e.EmployeeId == leave.EmployeeId);
                if (!employeeExists)
                    errors.Add($"Employee with ID {leave.EmployeeId} does not exist.");
            }

            // Check overlap only if dates are valid
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

            // Validate remarks
            if (!string.IsNullOrWhiteSpace(leave.Remarks))
            {
                if (leave.Remarks.Length > RemarksMaxLength)
                    errors.Add($"Remarks cannot exceed {RemarksMaxLength} characters.");
                else if (!_remarksRegex.IsMatch(leave.Remarks))
                    errors.Add(
                        "Remarks contain invalid characters. Only letters, numbers, spaces, and basic punctuation are allowed."
                    );
            }

            // Validate total days
            if (leave.TotalDays > MaxLeaveDurationDays)
                errors.Add($"Leave cannot exceed {MaxLeaveDurationDays} working days.");

            // Only non-sick leaves must be applied in advance
            if (
                leave.StartDate.Date == DateTime.Today
                && !string.IsNullOrWhiteSpace(leave.LeaveType)
                && !string.Equals(leave.LeaveType, "Sick", StringComparison.OrdinalIgnoreCase)
            )
            {
                errors.Add(
                    $"Non-sick leaves must be applied at least {MinAdvanceNoticeDays} day in advance."
                );
            }

            // Throw all errors together
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }

        private void ValidateLeaveDates(DateTime startDate, DateTime endDate)
        {
            // This method is now integrated into ValidateLeaveApplicationAsync
            // Kept for backward compatibility but not used
        }

        private void ValidateLeaveType(string leaveType)
        {
            // This method is now integrated into ValidateLeaveApplicationAsync
            // Kept for backward compatibility but not used
        }

        private void ValidateLeaveStatus(string leaveStatus)
        {
            if (string.IsNullOrWhiteSpace(leaveStatus))
                throw new ArgumentException("Leave status is required.");

            if (!_validLeaveStatuses.Contains(leaveStatus))
                throw new ArgumentException(
                    $"Invalid leave status '{leaveStatus}'. Valid statuses: {string.Join(", ", _validLeaveStatuses)}"
                );
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
                    $"Cannot change status from '{currentStatus}' to '{newStatus}'. "
                        + $"Allowed transitions: {string.Join(", ", allowedTransitions[currentStatus])}"
                );
            }
        }

        private void ValidateRemarks(string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
                return;

            if (remarks.Length > RemarksMaxLength)
                throw new ArgumentException(
                    $"Remarks cannot exceed {RemarksMaxLength} characters."
                );

            if (!_remarksRegex.IsMatch(remarks))
                throw new ArgumentException(
                    "Remarks contain invalid characters. Only letters, numbers, spaces, and basic punctuation are allowed."
                );
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

        // Keep this method for internal use if needed, but don't use it in ApplyForLeaveAsync
        private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            int totalDays = 0;
            DateTime current = startDate.Date;

            while (current <= endDate.Date)
            {
                if (
                    current.DayOfWeek != DayOfWeek.Saturday
                    && current.DayOfWeek != DayOfWeek.Sunday
                )
                {
                    totalDays++;
                }
                current = current.AddDays(1);
            }

            return totalDays;
        }

        #endregion
    }
}
