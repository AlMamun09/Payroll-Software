using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly ApplicationDbContext _context;

        public PayrollRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Payroll>> GetAllPayrollsAsync()
        {
            return await Task.FromResult(
                _context.Payrolls.OrderByDescending(p => p.PayPeriodStart).ToList()
            );
        }

        public async Task<Payroll?> GetPayrollByIdAsync(Guid payrollId)
        {
            return await Task.FromResult(
                _context.Payrolls.FirstOrDefault(p => p.PayrollId == payrollId)
            );
        }

        public async Task<bool> ExistsAsync(
            Guid employeeId,
            DateTime periodStart,
            DateTime periodEnd
        )
        {
            return await _context
                .Payrolls.AsNoTracking()
                .AnyAsync(p =>
                    p.EmployeeId == employeeId
                    && p.PayPeriodStart == periodStart
                    && p.PayPeriodEnd == periodEnd
                );
        }

        public async Task<Payroll> ProcessPayrollAsync(
            Guid employeeId,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken ct = default
        )
        {
            // Prevent duplicates
            if (await ExistsAsync(employeeId, periodStart.Date, periodEnd.Date))
                throw new InvalidOperationException(
                    "Payroll already exists for this employee and period."
                );

            // Get employee details
            var employee =
                await _context
                    .Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct)
                ?? throw new ArgumentException("Employee not found");

            // Step 1: Calculate total days between start and end date
            var totalDays = (periodEnd.Date - periodStart.Date).Days + 1;

            // Step 2: Calculate unpaid leave days
            var unpaidLeaveDays = await _context
                .Leaves.AsNoTracking()
                .Where(l =>
                    l.EmployeeId == employeeId
                    && l.LeaveStatus == "Approved"
                    && l.LeaveType == "Unpaid"
                    && l.StartDate.Date <= periodEnd.Date
                    && l.EndDate.Date >= periodStart.Date
                )
                .ToListAsync(ct);

            int totalUnpaidDays = 0;
            foreach (var leave in unpaidLeaveDays)
            {
                var leaveStart = leave.StartDate.Date < periodStart.Date
                    ? periodStart.Date
                    : leave.StartDate.Date;
                var leaveEnd = leave.EndDate.Date > periodEnd.Date
                    ? periodEnd.Date
                    : leave.EndDate.Date;
                totalUnpaidDays += (leaveEnd - leaveStart).Days + 1;
            }

            // Step 3: Calculate absent days (days not marked as Present in Attendance)
            var presentDays = await _context
                .Attendances.AsNoTracking()
                .Where(a =>
                    a.EmployeeId == employeeId
                    && a.AttendanceDate >= periodStart.Date
                    && a.AttendanceDate <= periodEnd.Date
                    && a.Status == "Present"
                )
                .Select(a => a.AttendanceDate.Date)
                .Distinct()
                .CountAsync(ct);

            // Get paid leave days (these count as present)
            var paidLeaveDays = await _context
                .Leaves.AsNoTracking()
                .Where(l =>
                    l.EmployeeId == employeeId
                    && l.LeaveStatus == "Approved"
                    && l.LeaveType != "Unpaid"
                    && l.StartDate.Date <= periodEnd.Date
                    && l.EndDate.Date >= periodStart.Date
                )
                .ToListAsync(ct);

            int totalPaidLeaveDays = 0;
            foreach (var leave in paidLeaveDays)
            {
                var leaveStart = leave.StartDate.Date < periodStart.Date
                    ? periodStart.Date
                    : leave.StartDate.Date;
                var leaveEnd = leave.EndDate.Date > periodEnd.Date
                    ? periodEnd.Date
                    : leave.EndDate.Date;
                totalPaidLeaveDays += (leaveEnd - leaveStart).Days + 1;
            }

            // Total working days = present days + paid leave days
            var totalWorkingDays = presentDays + totalPaidLeaveDays;

            // Absent days = total days - working days - unpaid leave days
            var absentDays = totalDays - totalWorkingDays - totalUnpaidDays;
            if (absentDays < 0)
                absentDays = 0;

            // Step 4: Calculate payable days
            var payableDays = totalDays - totalUnpaidDays - absentDays;
            if (payableDays < 0)
                payableDays = 0;

            // Step 5: Get BasicSalary from Employee
            var basicSalary = employee.BasicSalary;

            // Step 6: Calculate pro-rated salary based on payable days
            var dailyRate = totalDays > 0 ? basicSalary / totalDays : 0m;
            var proRatedSalary = Math.Round(dailyRate * payableDays, 2);

            // Step 7: Get active allowances and deductions
            var allowancesDeductions = await _context
                .AllowanceDeductions.AsNoTracking()
                .Where(ad =>
                    ad.IsActive
                    && ad.EffectiveFrom.Date <= periodEnd.Date
                    && (ad.EffectiveTo == null || ad.EffectiveTo.Value.Date >= periodStart.Date)
                    && (ad.IsCompanyWide || ad.EmployeeId == employeeId)
                )
                .ToListAsync(ct);

            decimal totalAllowances = 0m;
            decimal totalDeductions = 0m;

            foreach (var ad in allowancesDeductions)
            {
                decimal amount = 0m;

                if (ad.CalculationType == "Fixed")
                {
                    amount = ad.FixedAmount;
                }
                else if (ad.CalculationType == "Percentage")
                {
                    // Calculate percentage based on full basic salary
                    amount = Math.Round((ad.Percentage / 100m) * basicSalary, 2);
                }

                // Apply allowances only if present days >= 7
                // Deductions are always applied regardless of present days
                if (ad.AllowanceDeductionType == "Allowance")
                {
                    if (presentDays >= 7)
                    {
                        totalAllowances += amount;
                    }
                    // If present days < 7, allowance is not applied
                }
                else if (ad.AllowanceDeductionType == "Deduction")
                {
                    totalDeductions += amount;
                }
            }

            // Step 8: Calculate final net salary
            // Net Salary = Pro-rated Basic Salary + Allowances - Deductions
            var netSalary = proRatedSalary + totalAllowances - totalDeductions;

            // Create payroll record with all calculated values
            var payroll = new Payroll
            {
                PayrollId = Guid.NewGuid(),
                EmployeeId = employeeId,
                PayPeriodStart = periodStart.Date,
                PayPeriodEnd = periodEnd.Date,
                TotalDays = totalDays,
                PresentDays = presentDays,
                PaidLeaveDays = totalPaidLeaveDays,
                UnpaidLeaveDays = totalUnpaidDays,
                AbsentDays = absentDays,
                PayableDays = payableDays,
                BasicSalary = basicSalary,
                TotalAllowances = totalAllowances,
                TotalDeductions = totalDeductions,
                NetSalary = netSalary,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync(ct);
            return payroll;
        }

        public async Task UpdatePayrollAsync(Payroll payroll, string? updatedBy = null)
        {
            payroll.UpdatedAt = DateTime.UtcNow;
            payroll.UpdatedBy = updatedBy;

            _context.Payrolls.Update(payroll);
            await _context.SaveChangesAsync();
        }
    }
}
