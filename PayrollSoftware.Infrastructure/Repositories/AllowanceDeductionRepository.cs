using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class AllowanceDeductionRepository : IAllowanceDeductionRepository
    {
        private readonly ApplicationDbContext _context;

        // Validation constants
        private const int NameMinLength = 2;
        private const int NameMaxLength = 100;
        private const decimal MinPercentage = 0;
        private const decimal MaxPercentage = 100;
        private const decimal MinFixedAmount = 0;
        private const decimal MaxFixedAmount = 999999999.99m;

        private static readonly string[] AllowedTypes = new[] { "Allowance", "Deduction" };
        private static readonly string[] AllowedCalculationTypes = new[] { "Fixed", "Percentage" };

        public AllowanceDeductionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AllowanceDeduction>> GetAllAllowanceDeductionsAsync()
        {
            return await _context
                .AllowanceDeductions.OrderByDescending(a => a.EffectiveFrom)
                .ToListAsync();
        }

        public async Task<List<AllowanceDeduction>> GetAllActiveAllowanceDeductionsAsync()
        {
            var today = DateTime.Today;
            return await _context
                .AllowanceDeductions.Where(a =>
                    a.IsActive
                    && a.EffectiveFrom <= today
                    && (a.EffectiveTo == null || a.EffectiveTo >= today)
                )
                .OrderBy(a => a.AllowanceDeductionName)
                .ToListAsync();
        }

        public async Task<List<AllowanceDeduction>> GetCompanyWideAllowanceDeductionsAsync()
        {
            var today = DateTime.Today;
            return await _context
                .AllowanceDeductions.Where(a =>
                    a.IsCompanyWide
                    && a.IsActive
                    && a.EffectiveFrom <= today
                    && (a.EffectiveTo == null || a.EffectiveTo >= today)
                )
                .ToListAsync();
        }

        public async Task<List<AllowanceDeduction>> GetEmployeeSpecificAllowanceDeductionsAsync(
            Guid employeeId
        )
        {
            var today = DateTime.Today;
            return await _context
                .AllowanceDeductions.Where(a =>
                    a.EmployeeId == employeeId
                    && !a.IsCompanyWide
                    && a.IsActive
                    && a.EffectiveFrom <= today
                    && (a.EffectiveTo == null || a.EffectiveTo >= today)
                )
                .ToListAsync();
        }

        public async Task<AllowanceDeduction?> GetAllowanceDeductionByIdAsync(
            Guid allowanceDeductionId
        )
        {
            return await _context.AllowanceDeductions.FirstOrDefaultAsync(a =>
                a.AllowanceDeductionId == allowanceDeductionId
            );
        }

        public async Task AddAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction)
        {
            await ValidateAllowanceDeductionAsync(allowanceDeduction);

            _context.AllowanceDeductions.Add(allowanceDeduction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAllowanceDeductionAsync(AllowanceDeduction allowanceDeduction)
        {
            await ValidateAllowanceDeductionAsync(allowanceDeduction);

            _context.AllowanceDeductions.Update(allowanceDeduction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllowanceDeductionAsync(Guid allowanceDeductionId)
        {
            var allowanceDeduction = await GetAllowanceDeductionByIdAsync(allowanceDeductionId);
            if (allowanceDeduction != null)
            {
                // Check if this allowance/deduction is linked to any payroll records
                var isLinkedToPayroll = await _context.AllowanceDeductions.AnyAsync(a =>
                    a.AllowanceDeductionId == allowanceDeductionId && a.PayrollId != null
                );

                if (isLinkedToPayroll)
                {
                    throw new InvalidOperationException(
                        "Cannot delete this allowance/deduction as it is linked to payroll records. Please deactivate it instead."
                    );
                }

                _context.AllowanceDeductions.Remove(allowanceDeduction);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ValidateAllowanceDeductionAsync(AllowanceDeduction ad)
        {
            if (ad == null)
                throw new ArgumentNullException(nameof(ad), "Allowance/Deduction cannot be null.");

            var errors = new List<string>();

            // Normalize strings
            ad.AllowanceDeductionType = ad.AllowanceDeductionType?.Trim();
            ad.AllowanceDeductionName = ad.AllowanceDeductionName?.Trim();
            ad.CalculationType = ad.CalculationType?.Trim();

            // Validate Type
            if (string.IsNullOrWhiteSpace(ad.AllowanceDeductionType))
                errors.Add("Type is required.");
            else if (!AllowedTypes.Contains(ad.AllowanceDeductionType))
                errors.Add("Type must be either 'Allowance' or 'Deduction'.");

            // Validate Name
            if (string.IsNullOrWhiteSpace(ad.AllowanceDeductionName))
                errors.Add("Name is required.");
            else
            {
                if (
                    ad.AllowanceDeductionName.Length < NameMinLength
                    || ad.AllowanceDeductionName.Length > NameMaxLength
                )
                    errors.Add(
                        $"Name must be between {NameMinLength} and {NameMaxLength} characters."
                    );

                // Check for duplicates - same name, type, and scope (company-wide or employee-specific)
                var normalized = ad.AllowanceDeductionName.ToLower();
                var exists = await _context
                    .AllowanceDeductions.AsNoTracking()
                    .AnyAsync(a =>
                        a.AllowanceDeductionId != ad.AllowanceDeductionId
                        && a.AllowanceDeductionName != null
                        && a.AllowanceDeductionName.ToLower() == normalized
                        && a.AllowanceDeductionType == ad.AllowanceDeductionType
                        && a.IsCompanyWide == ad.IsCompanyWide
                        && (ad.IsCompanyWide || a.EmployeeId == ad.EmployeeId)
                    );

                if (exists)
                    errors.Add(
                        "An allowance/deduction with this name already exists for the same scope."
                    );
            }

            // Validate Calculation Type
            if (string.IsNullOrWhiteSpace(ad.CalculationType))
                errors.Add("Calculation Type is required.");
            else if (!AllowedCalculationTypes.Contains(ad.CalculationType))
                errors.Add("Calculation Type must be either 'Fixed' or 'Percentage'.");

            // Validate Percentage or Fixed Amount based on Calculation Type
            if (ad.CalculationType == "Percentage")
            {
                if (ad.Percentage < MinPercentage || ad.Percentage > MaxPercentage)
                    errors.Add(
                        $"Percentage must be between {MinPercentage}% and {MaxPercentage}%."
                    );

                // Clear fixed amount when using percentage
                ad.FixedAmount = 0;
            }
            else if (ad.CalculationType == "Fixed")
            {
                if (ad.FixedAmount < MinFixedAmount || ad.FixedAmount > MaxFixedAmount)
                    errors.Add(
                        $"Fixed Amount must be between {MinFixedAmount:C} and {MaxFixedAmount:C}."
                    );

                // Clear percentage when using fixed
                ad.Percentage = 0;
            }

            // Validate Effective Dates
            if (ad.EffectiveFrom == default)
                errors.Add("Effective From date is required.");
            else
                ad.EffectiveFrom = ad.EffectiveFrom.Date; // Normalize to date only

            if (ad.EffectiveTo.HasValue)
            {
                ad.EffectiveTo = ad.EffectiveTo.Value.Date; // Normalize to date only

                if (ad.EffectiveTo.Value < ad.EffectiveFrom)
                    errors.Add("Effective To date must be after Effective From date.");
            }

            // Validate Company-Wide vs Employee-Specific
            if (ad.IsCompanyWide)
            {
                // Company-wide should not have an employee ID
                ad.EmployeeId = null;
            }
            else
            {
                // Employee-specific must have an employee ID
                if (!ad.EmployeeId.HasValue || ad.EmployeeId == Guid.Empty)
                    errors.Add("Employee is required for employee-specific allowance/deduction.");
                else
                {
                    // Validate that employee exists
                    var employeeExists = await _context
                        .Employees.AsNoTracking()
                        .AnyAsync(e => e.EmployeeId == ad.EmployeeId.Value);

                    if (!employeeExists)
                        errors.Add("Selected employee does not exist.");
                }
            }

            // PayrollId validation (if set, must exist)
            if (ad.PayrollId.HasValue)
            {
                var payrollExists = await _context
                    .Payrolls.AsNoTracking()
                    .AnyAsync(p => p.PayrollId == ad.PayrollId.Value);

                if (!payrollExists)
                    errors.Add("Invalid payroll reference.");
            }

            // Throw all validation errors together
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }
    }
}
