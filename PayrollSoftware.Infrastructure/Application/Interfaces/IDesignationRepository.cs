using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IDesignationRepository
    {
        Task<List<Designation>> GetAllDesignationsAsync();
        Task<List<Designation>> GetAllActiveDesignationsAsync();
        Task<Designation?> GetDesignationByIdAsync(Guid designationId);
        Task AddDesignationAsync(Designation designation);
        Task UpdateDesignationAsync(Designation designation);
        Task DeleteDesignationAsync(Guid designationId);
    }
}
