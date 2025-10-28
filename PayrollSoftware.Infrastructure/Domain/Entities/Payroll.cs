using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Payroll
    {
        [Key]
        public Guid PayrollId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
