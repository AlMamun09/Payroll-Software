namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class DashboardDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int TotalPresentToday { get; set; }
        public int TotalAbsentToday { get; set; }
        public int TotalLateToday { get; set; }

        // Payroll Statistics
        public int TotalPayrollsThisMonth { get; set; }
        public int PendingPayments { get; set; }
        public int CompletedPayments { get; set; }
        public decimal TotalPayrollAmountThisMonth { get; set; }
    }
}
