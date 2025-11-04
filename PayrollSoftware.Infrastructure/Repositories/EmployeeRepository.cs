using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        public readonly ApplicationDbContext _context; 
        public EmployeeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await Task.FromResult(_context.Employees.ToList());
        }

        public async Task<Employee?> GetEmployeeByIdAsync(Guid employeeId)
        {
            return await Task.FromResult(_context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId));
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            await ValidateEmployeeAsync(employee, isNew: true);

            // Auto-generate numeric id and human readable EmployeeCode if needed
            if (employee.EmployeeNumericId <=0)
            {
                employee.EmployeeNumericId = await GetNextEmployeeNumericIdAsync();
            }

            if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
            {
                employee.EmployeeCode = FormatEmployeeCode(employee.EmployeeNumericId);
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            await ValidateEmployeeAsync(employee, isNew: false);

            // Keep existing numeric id if set; otherwise, set one
            if (employee.EmployeeNumericId <=0)
            {
                employee.EmployeeNumericId = await GetNextEmployeeNumericIdAsync();
            }

            if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
            {
                employee.EmployeeCode = FormatEmployeeCode(employee.EmployeeNumericId);
            }

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(Guid employeeId)
        {
            var employee = await GetEmployeeByIdAsync(employeeId);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        // --- Validation and business rules ---
        private static readonly string[] AllowedStatuses = new[]
        {
            "Currently Active",
            "Resigned"
        };

        private static readonly string[] AllowedPaymentSystems = new[]
        {
            "Bank Transfer",
            "Mobile Banking",
            "Cash Payment"
        };

        private async Task ValidateEmployeeAsync(Employee e, bool isNew)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            // Collect all validation errors instead of throwing on first error
            var errors = new List<string>();

            // Normalize strings (trim)
            e.FullName = e.FullName?.Trim();
            e.Gender = e.Gender?.Trim();
            e.EmploymentType = e.EmploymentType?.Trim();
            e.PaymentSystem = e.PaymentSystem?.Trim();
            e.AccountHolderName = e.AccountHolderName?.Trim();
            e.BankAndBranchName = e.BankAndBranchName?.Trim();
            e.BankAccountNumber = e.BankAccountNumber?.Trim();
            e.MobileNumber = e.MobileNumber?.Trim();
            e.Status = string.IsNullOrWhiteSpace(e.Status) ? "Currently Active" : e.Status.Trim();

            

            // FullName required and length
            if (string.IsNullOrWhiteSpace(e.FullName))
                errors.Add("Full Name is required.");
            else if (e.FullName.Length > 100)
                errors.Add("Full Name cannot exceed 100 characters.");

            // Gender required
            if (string.IsNullOrWhiteSpace(e.Gender))
                errors.Add("Gender is required.");

            // DateOfBirth required; ensure date-only normalization
            if (e.DateOfBirth == default)
                errors.Add("Date of Birth is required.");
            else
            {
                e.DateOfBirth = e.DateOfBirth.Date;

                // Age >=18 at joining
                if (e.JoiningDate != default)
                {
                    var ageAtJoining = GetAgeOn(e.DateOfBirth, e.JoiningDate);
                    if (ageAtJoining < 18)
                        errors.Add("Employee must be 18 years or older at the time of joining.");
                }
            }

            // JoiningDate required and date-only
            if (e.JoiningDate == default)
                errors.Add("Joining Date is required.");
            else
                e.JoiningDate = e.JoiningDate.Date;

            // Designation required and must exist
            if (e.DesignationId == Guid.Empty)
                errors.Add("Designation is required.");
            else
            {
                var designationExists = await _context.Designations.AsNoTracking().AnyAsync(d => d.DesignationId == e.DesignationId);
                if (!designationExists)
                    errors.Add("Invalid designation selected.");
            }

            // Department required and must exist
            if (e.DepartmentId == Guid.Empty)
                errors.Add("Department is required.");
            else
            {
                var deptExists = await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == e.DepartmentId);
                if (!deptExists)
                    errors.Add("Invalid department selected.");
            }

            // Optional Shift - if provided, must exist
            if (e.ShiftId.HasValue)
            {
                var shiftExists = await _context.Shifts.AsNoTracking().AnyAsync(s => s.ShiftId == e.ShiftId.Value);
                if (!shiftExists)
                    errors.Add("Invalid shift selected.");
            }

            // BasicSalary required and >=0
            if (e.BasicSalary < 0)
                errors.Add("Basic Salary cannot be negative.");

            // EmploymentType required
            if (string.IsNullOrWhiteSpace(e.EmploymentType))
                errors.Add("Employment Type is required.");

            // PaymentSystem validation and conditional fields
            if (string.IsNullOrWhiteSpace(e.PaymentSystem))
                errors.Add("Payment System is required.");
            else if (!AllowedPaymentSystems.Contains(e.PaymentSystem))
                errors.Add("Invalid payment system.");
            else
            {
                switch (e.PaymentSystem)
                {
                    case "Bank Transfer":
                        if (string.IsNullOrWhiteSpace(e.AccountHolderName))
                            errors.Add("Account Holder Name is required for Bank Transfer.");
                        if (string.IsNullOrWhiteSpace(e.BankAndBranchName))
                            errors.Add("Bank & Branch Name is required for Bank Transfer.");
                        if (string.IsNullOrWhiteSpace(e.BankAccountNumber))
                            errors.Add("Bank Account Number is required for Bank Transfer.");
                        else if (e.BankAccountNumber.Length > 50)
                            errors.Add("Bank Account Number cannot exceed 50 characters.");
                        // When bank transfer, mobile number should be empty or ignored
                        e.MobileNumber = null;
                        break;

                    case "Mobile Banking":
                        if (string.IsNullOrWhiteSpace(e.MobileNumber))
                            errors.Add("Mobile Number is required for Mobile Banking.");
                        else if (!IsValidPhoneNumber(e.MobileNumber))
                            errors.Add("Mobile Number is not in a valid format.");
                        // Bank-related fields should be empty or ignored
                        e.AccountHolderName = null;
                        e.BankAndBranchName = null;
                        e.BankAccountNumber = null;
                        break;

                    case "Cash Payment":
                        // Hide/clear all payment-related fields
                        e.AccountHolderName = null;
                        e.BankAndBranchName = null;
                        e.BankAccountNumber = null;
                        e.MobileNumber = null;
                        break;
                }
            }

            // Status required and allowed values
            if (string.IsNullOrWhiteSpace(e.Status))
                e.Status = "Currently Active";
            else if (!AllowedStatuses.Contains(e.Status))
                errors.Add("Invalid status.");

            // If there are any validation errors, throw with all messages
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }

        private static int GetAgeOn(DateTime dob, DateTime onDate)
        {
            int age = onDate.Year - dob.Year;
            if (dob.Date > onDate.AddYears(-age)) age--;
            return age;
        }

        private async Task<int> GetNextEmployeeNumericIdAsync()
        {
            var currentMax = await _context.Employees.AsNoTracking().Select(x => (int?)x.EmployeeNumericId).MaxAsync();
            return (currentMax ??0) +1;
        }

        private static string FormatEmployeeCode(int numericId)
        {
            // EMP-001 style, grows beyond999 as needed
            return $"EMP{numericId:D3}";
        }

        private static bool IsValidPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            // Simple international phone pattern: +, digits, spaces, dashes allowed,7-20 chars
            var re = new Regex(@"^\+?[0-9\s-]{7,20}$");
            return re.IsMatch(phone);
        }
    }
}
