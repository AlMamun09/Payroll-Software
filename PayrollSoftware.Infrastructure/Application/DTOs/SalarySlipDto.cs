using System;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class SalarySlipDto
    {
        public Guid SalarySlipId { get; set; }
        public Guid PayrollId { get; set; }
        public Guid EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedDate { get; set; }
    }
}
