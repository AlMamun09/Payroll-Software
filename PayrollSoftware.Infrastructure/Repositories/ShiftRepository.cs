using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly ApplicationDbContext _context;

        // Validation rules
        private const int ShiftNameMinLength = 2;
        private const int ShiftNameMaxLength = 100;

        public ShiftRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Shift>> GetAllShiftsAsync()
        {
            return await Task.FromResult(_context.Shifts.ToList());
        }

        public async Task<List<Shift>> GetAllActiveShiftsAsync()
        {
            return await Task.FromResult(_context.Shifts.Where(s => s.IsActive).ToList());
        }

        public async Task<Shift?> GetShiftByIdAsync(Guid shiftId)
        {
            return await Task.FromResult(_context.Shifts.FirstOrDefault(s => s.ShiftId == shiftId));
        }

        public async Task AddShiftAsync(Shift shift)
        {
            await ValidateShiftAsync(shift);

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateShiftAsync(Shift shift)
        {
            await ValidateShiftAsync(shift);

            _context.Shifts.Update(shift);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteShiftAsync(Guid shiftId)
        {
            var shift = await GetShiftByIdAsync(shiftId);
            if (shift != null)
            {
                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();
            }
        }

        // Centralized validation logic for Shift entity
        private async Task ValidateShiftAsync(Shift shift)
        {
            if (shift == null)
                throw new ArgumentNullException(nameof(shift));

            // Collect all validation errors
            var errors = new List<string>();

            // Name validations
            var name = shift.ShiftName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Shift name is required.");
            else
            {
                if (name.Length < ShiftNameMinLength || name.Length > ShiftNameMaxLength)
                    errors.Add($"Shift name must be between {ShiftNameMinLength} and {ShiftNameMaxLength} characters.");

                if (!name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                    errors.Add("Shift name must contain only letters and spaces.");

                // Duplicate check (case-insensitive, excluding self on update)
                var normalized = name.ToLower();
                var exists = await _context.Shifts
                    .AsNoTracking()
                    .AnyAsync(s => s.ShiftId != shift.ShiftId &&
                                   s.ShiftName != null &&
                                   s.ShiftName.ToLower() == normalized);
                if (exists)
                    errors.Add("A shift with the same name already exists.");

                // Normalize persisted values
                shift.ShiftName = name;
            }

            // Time validations
            // zero-length shifts
            if (shift.StartTime == shift.EndTime)
                errors.Add("Start and end times can't be the same.");

            // Throw all errors together
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }
    }
}
