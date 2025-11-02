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

            // Employee Code uniqueness (if provided)
            if (!string.IsNullOrWhiteSpace(e.EmployeeCode))
            {
                var codeExists = await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(x => x.EmployeeId != e.EmployeeId && x.EmployeeCode != null && x.EmployeeCode == e.EmployeeCode);
                if (codeExists)
                    throw new InvalidOperationException("Employee Code already exists.");
            }

            // FullName required and length
            if (string.IsNullOrWhiteSpace(e.FullName))
                throw new ArgumentException("Full Name is required.", nameof(e.FullName));
            if (e.FullName.Length >100)
                throw new ArgumentException("Full Name cannot exceed100 characters.", nameof(e.FullName));

            // Gender required
            if (string.IsNullOrWhiteSpace(e.Gender))
                throw new ArgumentException("Gender is required.", nameof(e.Gender));

            // DateOfBirth required; ensure date-only normalization
            if (e.DateOfBirth == default)
                throw new ArgumentException("Date of Birth is required.", nameof(e.DateOfBirth));
            e.DateOfBirth = e.DateOfBirth.Date;

            // JoiningDate required and date-only
            if (e.JoiningDate == default)
                throw new ArgumentException("Joining Date is required.", nameof(e.JoiningDate));
            e.JoiningDate = e.JoiningDate.Date;

            // Age >=18 at joining
            var ageAtJoining = GetAgeOn(e.DateOfBirth, e.JoiningDate);
            if (ageAtJoining <18)
                throw new ArgumentException("Employee must be18 years or older at the time of joining.", nameof(e.JoiningDate));

            // Designation required and must exist
            if (e.DesignationId == Guid.Empty)
                throw new ArgumentException("Designation is required.", nameof(e.DesignationId));
            var designationExists = await _context.Designations.AsNoTracking().AnyAsync(d => d.DesignationId == e.DesignationId);
            if (!designationExists)
                throw new ArgumentException("Invalid designation selected.", nameof(e.DesignationId));

            // Department required and must exist
            if (e.DepartmentId == Guid.Empty)
                throw new ArgumentException("Department is required.", nameof(e.DepartmentId));
            var deptExists = await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == e.DepartmentId);
            if (!deptExists)
                throw new ArgumentException("Invalid department selected.", nameof(e.DepartmentId));

            // Optional Shift - if provided, must exist
            if (e.ShiftId.HasValue)
            {
                var shiftExists = await _context.Shifts.AsNoTracking().AnyAsync(s => s.ShiftId == e.ShiftId.Value);
                if (!shiftExists)
                    throw new ArgumentException("Invalid shift selected.", nameof(e.ShiftId));
            }

            // BasicSalary required and >=0
            if (e.BasicSalary <0)
                throw new ArgumentException("Basic Salary is required and cannot be negative.", nameof(e.BasicSalary));

            // EmploymentType required
            if (string.IsNullOrWhiteSpace(e.EmploymentType))
                throw new ArgumentException("Employment Type is required.", nameof(e.EmploymentType));

            // PaymentSystem validation and conditional fields
            if (string.IsNullOrWhiteSpace(e.PaymentSystem))
                throw new ArgumentException("Payment System is required.", nameof(e.PaymentSystem));

            if (!AllowedPaymentSystems.Contains(e.PaymentSystem))
                throw new ArgumentException("Invalid payment system.", nameof(e.PaymentSystem));

            switch (e.PaymentSystem)
            {
                case "Bank Transfer":
                    if (string.IsNullOrWhiteSpace(e.AccountHolderName))
                        throw new ArgumentException("Account Holder Name is required for Bank Transfer.", nameof(e.AccountHolderName));
                    if (string.IsNullOrWhiteSpace(e.BankAndBranchName))
                        throw new ArgumentException("Bank & Branch Name is required for Bank Transfer.", nameof(e.BankAndBranchName));
                    if (string.IsNullOrWhiteSpace(e.BankAccountNumber))
                        throw new ArgumentException("Bank Account Number is required for Bank Transfer.", nameof(e.BankAccountNumber));
                    if (e.BankAccountNumber.Length >50)
                        throw new ArgumentException("Bank Account Number cannot exceed50 characters.", nameof(e.BankAccountNumber));
                    // When bank transfer, mobile number should be empty or ignored
                    e.MobileNumber = null;
                    break;

                case "Mobile Banking":
                    if (string.IsNullOrWhiteSpace(e.MobileNumber))
                        throw new ArgumentException("Mobile Number is required for Mobile Banking.", nameof(e.MobileNumber));
                    if (!IsValidPhoneNumber(e.MobileNumber))
                        throw new ArgumentException("Mobile Number is not in a valid format.", nameof(e.MobileNumber));
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

            // Status required and allowed values
            if (string.IsNullOrWhiteSpace(e.Status))
                e.Status = "Currently Active";
            if (!AllowedStatuses.Contains(e.Status))
                throw new ArgumentException("Invalid status.", nameof(e.Status));
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
