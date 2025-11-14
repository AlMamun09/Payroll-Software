using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface ILookupRepository
    {
        Task<IEnumerable<Lookup>> GetAllLookupsAsync();
        Task<IEnumerable<Lookup>> GetLookupsByTypeAsync(string lookupType);
        Task<Lookup?> GetLookupByIdAsync(Guid lookupId);
        Task AddLookupAsync(Lookup lookup);
        Task UpdateLookupAsync(Lookup lookup);
        Task DeleteLookupAsync(Guid lookupId);
    }
}
