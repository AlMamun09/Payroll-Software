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
        Task AddShiftAsync(Shift shift);
        Task UpdateShiftAsync(Shift shift);
        Task DeleteShiftAsync(Guid shiftId);
    }
}
