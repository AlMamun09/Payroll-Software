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
    public class DesignationRepository : IDesignationRepository
    {
        private readonly ApplicationDbContext _context;
        // Validation rules
        private const int DesignationNameMinLength = 2;
        private const int DesignationNameMaxLength = 100;
        public DesignationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Designation>> GetAllDesignationsAsync()
        {
            return await Task.FromResult(_context.Designations.ToList());
        }

        public async Task<List<Designation>> GetAllActiveDesignationsAsync()
        {
            return await Task.FromResult(_context.Designations.Where(d => d.IsActive).ToList());
        }

        public async Task<Designation?> GetDesignationByIdAsync(Guid designationId)
        {
            return await Task.FromResult(_context.Designations.FirstOrDefault(d => d.DesignationId == designationId));
        }

        public async Task AddDesignationAsync(Designation designation)
        {
            await ValidateDesignationAsync(designation);

            _context.Designations.Add(designation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDesignationAsync(Designation designation)
        {
            await ValidateDesignationAsync(designation);

            _context.Designations.Update(designation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDesignationAsync(Guid designationId)
        {
            var designation = await _context.Designations.FindAsync(designationId);
            if (designation != null)
            {
                _context.Designations.Remove(designation);
                await _context.SaveChangesAsync();
            }
        }

        // Centralized validation logic for Designation entity
        public async Task ValidateDesignationAsync(Designation designation)
        {
            if (designation == null)
                throw new ArgumentNullException(nameof(designation), "Designation cannot be null.");

            // Collect all validation errors
            var errors = new List<string>();

            var name = designation.DesignationName?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Designation name cannot be null or empty.");
            else
            {
                if (name.Length < DesignationNameMinLength || name.Length > DesignationNameMaxLength)
                    errors.Add($"Designation name must be between {DesignationNameMinLength} and {DesignationNameMaxLength} characters.");

                if (!name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                    errors.Add("Designation name can only contain alphabetic characters and spaces.");

                // Check for duplicates
                var normalized = name.ToLower();
                var exists = await _context.Designations
                    .AsNoTracking()
                    .AnyAsync(d => d.DesignationId != designation.DesignationId &&
                                   d.DesignationName != null &&
                                   d.DesignationName.ToLower() == normalized);

                if (exists)
                    errors.Add("Designation name already exists.");

                designation.DesignationName = name;
            }

            // Throw all errors together
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }
    }
}
