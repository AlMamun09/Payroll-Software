using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly ApplicationDbContext _context;
        // Validation rules
        private const int DepartmentNameMinLength = 2;
        private const int DepartmentNameMaxLength = 100;

        public DepartmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            return await Task.FromResult(_context.Departments.ToList());
        }

        public async Task<List<Department>> GetAllActiveDepartmentsAsync()
        {
            return await Task.FromResult(_context.Departments.Where(d => d.IsActive).ToList());
        }

        public async Task<Department?> GetDepartmentByIdAsync(Guid departmentId)
        {
            return await Task.FromResult(_context.Departments.FirstOrDefault(d => d.DepartmentId == departmentId));
        }

        public async Task AddDepartmentAsync(Department department)
        {
            await ValidateDepartmentAsync(department);

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDepartmentAsync(Department department)
        {
            await ValidateDepartmentAsync(department);

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDepartmentAsync(Guid departmentId)
        {
            var department = await GetDepartmentByIdAsync(departmentId);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }

        // Centralized validation logic for Department entity
        private async Task ValidateDepartmentAsync(Department department)
        {
            if (department == null)
                throw new ArgumentNullException(nameof(department));

            // Collect all validation errors
            var errors = new List<string>();

            var name = department.DepartmentName?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Department name is required.");
            else
            {
                if (name.Length < DepartmentNameMinLength || name.Length > DepartmentNameMaxLength)
                    errors.Add($"Department name must be between {DepartmentNameMinLength} and {DepartmentNameMaxLength} characters.");

                if (!name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                    errors.Add("Department name must contain only letters and spaces.");

                // Duplicate check (case-insensitive, excluding self on update)
                var normalized = name.ToLower();
                var exists = await _context.Departments
                    .AsNoTracking()
                    .AnyAsync(d => d.DepartmentId != department.DepartmentId &&
                                   d.DepartmentName != null &&
                                   d.DepartmentName.ToLower() == normalized);

                if (exists)
                    errors.Add("A department with the same name already exists.");

                // persist normalized value
                department.DepartmentName = name;
            }

            // Throw all errors together
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }
    }
}
