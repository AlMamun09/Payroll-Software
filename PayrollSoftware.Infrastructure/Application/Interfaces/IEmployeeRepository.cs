using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(Guid id);
        Task<Employee?> GetByEmployeeCodeAsync(string employeeCode);
        Task<Employee?> GetByNumericIdAsync(int numericId);
        Task<IEnumerable<Employee>> GetAllAsync();
        Task<Employee> CreateAsync(Employee employee);
        Task<Employee> UpdateAsync(Employee employee);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> EmployeeCodeExistsAsync(string employeeCode);
        Task<bool> EmployeeNumericIdExistsAsync(int numericId);
        Task<bool> IsEmployeeActiveAsync(Guid employeeId);
        Task<string> GenerateEmployeeCodeAsync();
        Task<bool> HasActiveAttendanceAsync(Guid employeeId);
        Task<bool> HasActiveLeavesAsync(Guid employeeId);
    }
}