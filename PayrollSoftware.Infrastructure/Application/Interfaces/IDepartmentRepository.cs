using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<List<Department>> GetAllDepartmentsAsync();
        Task<List<Department>> GetAllActiveDepartmentsAsync();
        Task<Department?> GetDepartmentByIdAsync(Guid departmentId);
        Task AddDepartmentAsync(Department department);
        Task UpdateDepartmentAsync(Department department);
        Task DeleteDepartmentAsync(Guid departmentId);
    }
}
