using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class SalarySlipRepository : ISalarySlipRepository
    {
        private readonly ApplicationDbContext _context;

        public SalarySlipRepository(ApplicationDbContext context)
        {
      _context = context;
        }

    public async Task<SalarySlip?> GetSalarySlipByIdAsync(Guid salarySlipId)
        {
     return await _context.SalarySlips
    .AsNoTracking()
       .FirstOrDefaultAsync(s => s.SalarySlipId == salarySlipId);
      }

        public async Task<SalarySlip?> GetSalarySlipByPayrollIdAsync(Guid payrollId)
        {
          return await _context.SalarySlips
     .AsNoTracking()
.FirstOrDefaultAsync(s => s.PayrollId == payrollId);
  }

     public async Task<List<SalarySlip>> GetAllSalarySlipsAsync()
     {
            return await _context.SalarySlips
          .OrderByDescending(s => s.GeneratedDate)
   .ToListAsync();
        }

  public async Task<List<SalarySlip>> GetSalarySlipsByEmployeeIdAsync(Guid employeeId)
 {
            return await _context.SalarySlips
  .Where(s => s.EmployeeId == employeeId)
           .OrderByDescending(s => s.GeneratedDate)
         .ToListAsync();
        }

        public async Task<SalarySlip> CreateSalarySlipAsync(SalarySlip salarySlip)
   {
            _context.SalarySlips.Add(salarySlip);
      await _context.SaveChangesAsync();
            return salarySlip;
        }

        public async Task UpdateSalarySlipAsync(SalarySlip salarySlip)
        {
            _context.SalarySlips.Update(salarySlip);
     await _context.SaveChangesAsync();
        }

        public async Task DeleteSalarySlipAsync(Guid salarySlipId)
{
            var salarySlip = await _context.SalarySlips.FindAsync(salarySlipId);
            if (salarySlip != null)
          {
    _context.SalarySlips.Remove(salarySlip);
     await _context.SaveChangesAsync();
            }
        }
    }
}
