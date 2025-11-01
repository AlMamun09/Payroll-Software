using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Employee
    {
        [Key]
        public Guid EmployeeId { get; set; }
        public Guid DesignationId { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? ShiftId { get; set; }
        public int EmployeeNumericId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime JoiningDate { get; set; }
        public decimal BasicSalary { get; set; }
        public string? EmploymentType { get; set; }
        public string? PaymentSystem { get; set; }
        public string? AccountHolderName { get; set; }
        public string? BankAndBranchName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? Status { get; set; }
    }
}
