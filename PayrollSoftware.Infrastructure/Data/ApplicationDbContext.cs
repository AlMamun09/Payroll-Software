using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Infrastructure.Domain.Entities;
using PayrollSoftware.Infrastructure.Identity;

namespace PayrollSoftware.Data
{
    public class ApplicationDbContext : IdentityDbContext<PayrollUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<SalarySlip> SalarySlips { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<AllowanceDeduction> AllowanceDeductions { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Lookup> Lookups { get; set; }
    }
}
