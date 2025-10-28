using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class AllowanceDeductionDto
    {
        public Guid AllowanceDeductionId { get; set; }
        public string? AllowanceDeductionType { get; set; }
        public string? AllowanceDeductionName { get; set; }
        public string? CalculationType { get; set; }
        public decimal Percentage { get; set; }
        public decimal FixedAmount { get; set; }
        public DateTime EffectiveFrom { get; set; }
    }
}
