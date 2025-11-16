using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IShiftRepository
    {
        Task<List<Shift>> GetAllShiftsAsync();
        Task<List<Shift>> GetAllActiveShiftsAsync();
        Task<Shift?> GetShiftByIdAsync(Guid shiftId);
        Task AddShiftAsync(Shift shift, string? createdBy = null);
        Task UpdateShiftAsync(Shift shift, string? updatedBy = null);
        Task DeactivateShiftAsync(Guid shiftId, string? updatedBy = null);
        Task ActivateShiftAsync(Guid shiftId, string? updatedBy = null);
    }
}
