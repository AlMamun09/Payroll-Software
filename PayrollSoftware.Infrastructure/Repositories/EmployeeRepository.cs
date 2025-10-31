using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using PayrollSoftware.Data; // ← Use the correct namespace

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByIdAsync(Guid id)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
        }

        public async Task<Employee?> GetByEmployeeCodeAsync(string employeeCode)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);
        }

        public async Task<Employee?> GetByNumericIdAsync(int numericId)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumericId == numericId);
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .Where(e => e.Status != "Resigned")
                .OrderBy(e => e.EmployeeNumericId)
                .ToListAsync();
        }

        public async Task<Employee> CreateAsync(Employee employee)
        {
            await ValidateEmployeeForCreationAsync(employee);

            if (string.IsNullOrEmpty(employee.EmployeeCode))
            {
                employee.EmployeeCode = await GenerateEmployeeCodeAsync();
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return employee;
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            await ValidateEmployeeForUpdateAsync(employee);

            var existingEmployee = await GetByIdAsync(employee.EmployeeId);
            if (existingEmployee == null)
            {
                throw new ArgumentException($"Employee with ID {employee.EmployeeId} not found");
            }

            // Update properties
            existingEmployee.FullName = employee.FullName;
            existingEmployee.Gender = employee.Gender;
            existingEmployee.Designation = employee.Designation;
            existingEmployee.DepartmentId = employee.DepartmentId;
            existingEmployee.JoiningDate = employee.JoiningDate;
            existingEmployee.BasicSalary = employee.BasicSalary;
            existingEmployee.EmploymentType = employee.EmploymentType;
            existingEmployee.PaymentSystem = employee.PaymentSystem;
            existingEmployee.AccountHolderName = employee.AccountHolderName;
            existingEmployee.BankAndBranchName = employee.BankAndBranchName;
            existingEmployee.BankAccountNumber = employee.BankAccountNumber;
            existingEmployee.MobileNumber = employee.MobileNumber;
            existingEmployee.Status = employee.Status;
            existingEmployee.ShiftId = employee.ShiftId;

            await _context.SaveChangesAsync();

            return existingEmployee;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var employee = await GetByIdAsync(id);
            if (employee == null)
            {
                return false;
            }

            if (await HasActiveAttendanceAsync(id))
            {
                throw new InvalidOperationException("Cannot delete employee with active attendance records");
            }

            if (await HasActiveLeavesAsync(id))
            {
                throw new InvalidOperationException("Cannot delete employee with active leave records");
            }

            employee.Status = "Resigned";
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EmployeeCodeExistsAsync(string employeeCode)
        {
            return await _context.Employees
                .AnyAsync(e => e.EmployeeCode == employeeCode && e.Status != "Resigned");
        }

        public async Task<bool> EmployeeNumericIdExistsAsync(int numericId)
        {
            return await _context.Employees
                .AnyAsync(e => e.EmployeeNumericId == numericId && e.Status != "Resigned");
        }

        public async Task<bool> IsEmployeeActiveAsync(Guid employeeId)
        {
            var employee = await GetByIdAsync(employeeId);
            return employee != null && employee.Status == "Currently Active";
        }

        public async Task<string> GenerateEmployeeCodeAsync()
        {
            var lastEmployee = await _context.Employees
                .OrderByDescending(e => e.EmployeeNumericId)
                .FirstOrDefaultAsync();

            var nextNumericId = (lastEmployee?.EmployeeNumericId ?? 0) + 1;
            return $"EMP-{nextNumericId:D3}";
        }

        public async Task<bool> HasActiveAttendanceAsync(Guid employeeId)
        {
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            return await _context.Attendances
                .AnyAsync(a => a.EmployeeId == employeeId && a.AttendanceDate >= thirtyDaysAgo);
        }

        public async Task<bool> HasActiveLeavesAsync(Guid employeeId)
        {
            var today = DateTime.Today;
            return await _context.Leaves
                .AnyAsync(l => l.EmployeeId == employeeId &&
                              l.Status == "Approved" &&
                              l.StartDate <= today &&
                              l.EndDate >= today);
        }

        // PRIVATE VALIDATION METHODS
        private async Task ValidateEmployeeForCreationAsync(Employee employee)
        {
            if (string.IsNullOrWhiteSpace(employee.FullName))
                throw new ArgumentException("Full name is required");

            if (employee.DateOfBirth == default)
                throw new ArgumentException("Date of birth is required");

            if (employee.JoiningDate == default)
                throw new ArgumentException("Joining date is required");

            if (employee.DepartmentId == Guid.Empty)
                throw new ArgumentException("Department is required");

            var ageAtJoining = CalculateAgeAtDate(employee.DateOfBirth, employee.JoiningDate);
            if (ageAtJoining < 18)
                throw new ArgumentException("Employee must be at least 18 years old at joining date");

            if (employee.JoiningDate > DateTime.Today)
                throw new ArgumentException("Joining date cannot be in the future");

            if (employee.BasicSalary < 0)
                throw new ArgumentException("Basic salary cannot be negative");

            ValidatePaymentSystemFields(employee);

            if (await EmployeeNumericIdExistsAsync(employee.EmployeeNumericId))
                throw new ArgumentException($"Employee numeric ID {employee.EmployeeNumericId} already exists");
        }

        private async Task ValidateEmployeeForUpdateAsync(Employee employee)
        {
            var existingEmployee = await GetByIdAsync(employee.EmployeeId);
            if (existingEmployee == null)
                throw new ArgumentException("Employee not found");

            if (string.IsNullOrWhiteSpace(employee.FullName))
                throw new ArgumentException("Full name is required");

            if (existingEmployee.DateOfBirth != employee.DateOfBirth)
                throw new ArgumentException("Date of birth cannot be modified");

            var ageAtJoining = CalculateAgeAtDate(existingEmployee.DateOfBirth, employee.JoiningDate);
            if (ageAtJoining < 18)
                throw new ArgumentException("Employee must be at least 18 years old at joining date");

            if (employee.JoiningDate > DateTime.Today)
                throw new ArgumentException("Joining date cannot be in the future");

            if (employee.BasicSalary < 0)
                throw new ArgumentException("Basic salary cannot be negative");

            ValidatePaymentSystemFields(employee);

            if (existingEmployee.EmployeeNumericId != employee.EmployeeNumericId &&
                await EmployeeNumericIdExistsAsync(employee.EmployeeNumericId))
            {
                throw new ArgumentException($"Employee numeric ID {employee.EmployeeNumericId} already exists");
            }
        }

        private void ValidatePaymentSystemFields(Employee employee)
        {
            switch (employee.PaymentSystem?.ToLower())
            {
                case "bank transfer":
                    if (string.IsNullOrWhiteSpace(employee.AccountHolderName))
                        throw new ArgumentException("Account holder name is required for bank transfer");
                    if (string.IsNullOrWhiteSpace(employee.BankAccountNumber))
                        throw new ArgumentException("Account number is required for bank transfer");
                    if (string.IsNullOrWhiteSpace(employee.BankAndBranchName))
                        throw new ArgumentException("Bank and branch name is required for bank transfer");
                    break;

                case "mobile banking":
                    if (string.IsNullOrWhiteSpace(employee.MobileNumber))
                        throw new ArgumentException("Mobile number is required for mobile banking");
                    if (!IsValidMobileNumber(employee.MobileNumber))
                        throw new ArgumentException("Invalid mobile number format");
                    break;

                case "cash payment":
                    employee.AccountHolderName = null;
                    employee.BankAccountNumber = null;
                    employee.BankAndBranchName = null;
                    employee.MobileNumber = null;
                    break;

                case null:
                    throw new ArgumentException("Payment system is required");

                default:
                    throw new ArgumentException("Invalid payment system selected");
            }
        }

        private bool IsValidMobileNumber(string mobileNumber)
        {
            return !string.IsNullOrWhiteSpace(mobileNumber) &&
                   mobileNumber.Length >= 10 &&
                   mobileNumber.All(char.IsDigit);
        }

        private int CalculateAgeAtDate(DateTime dateOfBirth, DateTime atDate)
        {
            var age = atDate.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > atDate.AddYears(-age))
                age--;
            return age;
        }
    }
}