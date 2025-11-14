using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class LookupRepository : ILookupRepository
    {
        private readonly ApplicationDbContext _context;

        public LookupRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Lookup>> GetAllLookupsAsync()
        {
            return await _context
                .Lookups.Where(l => l.IsActive)
                .OrderBy(l => l.LookupType)
                .ThenBy(l => l.DisplayOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lookup>> GetLookupsByTypeAsync(string lookupType)
        {
            return await _context
                .Lookups.Where(l => l.LookupType == lookupType && l.IsActive)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Lookup?> GetLookupByIdAsync(Guid lookupId)
        {
            return await _context.Lookups.FirstOrDefaultAsync(l =>
                l.LookupId == lookupId && l.IsActive
            );
        }

        public async Task AddLookupAsync(Lookup lookup)
        {
            if (string.IsNullOrWhiteSpace(lookup.LookupType))
                throw new ArgumentException("LookupType cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(lookup.LookupValue))
                throw new ArgumentException("LookupValue cannot be null or empty.");

            var exists = await _context.Lookups.AnyAsync(l =>
                l.LookupType == lookup.LookupType && l.LookupValue == lookup.LookupValue
            );

            if (exists)
                throw new InvalidOperationException(
                    $"Lookup '{lookup.LookupValue}' already exists for type '{lookup.LookupType}'"
                );

            lookup.CreatedAt = DateTime.UtcNow;
            lookup.IsActive = true;

            await _context.Lookups.AddAsync(lookup);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLookupAsync(Lookup lookup)
        {
            var existing = await _context.Lookups.FindAsync(lookup.LookupId);
            if (existing == null)
                throw new InvalidOperationException("Lookup not found");

            existing.LookupValue = lookup.LookupValue;
            existing.DisplayOrder = lookup.DisplayOrder;
            existing.IsActive = lookup.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteLookupAsync(Guid lookupId)
        {
            var lookup = await _context.Lookups.FindAsync(lookupId);
            if (lookup == null)
                throw new InvalidOperationException("Lookup not found");

            lookup.IsActive = false;
            _context.Lookups.Update(lookup);
            await _context.SaveChangesAsync();
        }
    }
}
