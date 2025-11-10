using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Employee
{
    [Authorize(Roles = "Employee")]
    public class DashboardModel : PageModel
    {
        private readonly IDealService _dealService;
        private readonly ICustomerService _customerService;

        public DashboardModel(IDealService dealService, ICustomerService customerService)
        {
            _dealService = dealService;
            _customerService = customerService;
        }

        // ==== Thống kê tổng quan ====
        public int RunningDeals { get; set; }
        public int WonDeals { get; set; }
        public int LostDeals { get; set; }
        public int PotentialCustomers { get; set; }
        public decimal TotalDealValue { get; set; }
        public decimal AverageDealValue { get; set; }
        public double WinRate { get; set; }

        // ==== Gần deadline ====
        public List<Models.Deal> NearDeadlineDeals { get; set; } = new();

        // ==== Biểu đồ 1: Deal theo giai đoạn ====
        public List<string> DealStageLabels { get; set; } = new();
        public List<int> DealStageCounts { get; set; } = new();

        // ==== Biểu đồ 2: Khách hàng theo ngành nghề ====
        public List<string> CustomerIndustryLabels { get; set; } = new();
        public List<int> CustomerIndustryCounts { get; set; } = new();

        // ==== Biểu đồ 3: Doanh thu theo tháng ====
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthlyRevenue { get; set; } = new();

        // ==== Biểu đồ 4: Tỉ lệ thắng theo tháng ====
        public List<double> MonthlyWinRate { get; set; } = new();

        // ==== Biểu đồ 5: Phân bố giá trị Deal (Small/Medium/Large) ====
        public List<string> DealSizeLabels { get; set; } = new() { "Nhỏ (<10 triệu)", "Vừa (10–50 triệu)", "Lớn (>50 triệu)" };
        public List<int> DealSizeCounts { get; set; } = new();

        // ==== Biểu đồ 6: Số khách hàng mới theo tháng ====
        public List<int> NewCustomersPerMonth { get; set; } = new();

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            var employeeId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
            var deals = (await _dealService.GetDealsByEmployeeAsync(employeeId)).Where(d => !d.IsDeleted).ToList();
            var customers = (await _customerService.GetCustomersForUserAsync(employeeId)).Where(c => !c.IsDeleted).ToList();

            // ==== Tổng quan ====
            RunningDeals = deals.Count(d => d.Stage != "Closed Won" && d.Stage != "Closed Lost");
            WonDeals = deals.Count(d => d.Stage == "Closed Won");
            LostDeals = deals.Count(d => d.Stage == "Closed Lost");
            PotentialCustomers = customers.Count;
            TotalDealValue = deals.Sum(d => d.Value);
            AverageDealValue = deals.Any() ? deals.Average(d => d.Value) : 0;
            WinRate = deals.Any() ? Math.Round((double)WonDeals / deals.Count * 100, 2) : 0;

            // ==== Deal gần deadline (7 ngày tới) ====
            NearDeadlineDeals = (await _dealService.GetDealsNearDeadlineAsync(employeeId, 7)).ToList();

            // ==== Biểu đồ 1: Deal theo Stage ====
            var groupedDeals = deals
                .GroupBy(d => d.Stage ?? "Không xác định")
                .Select(g => new { Stage = g.Key, Count = g.Count() })
                .ToList();
            DealStageLabels = groupedDeals.Select(g => g.Stage).ToList();
            DealStageCounts = groupedDeals.Select(g => g.Count).ToList();

            // ==== Biểu đồ 2: Khách hàng theo ngành ====
            var groupedCustomers = customers
                .GroupBy(c => string.IsNullOrEmpty(c.Industry) ? "Chưa xác định" : c.Industry)
                .Select(g => new { Industry = g.Key, Count = g.Count() })
                .ToList();
            CustomerIndustryLabels = groupedCustomers.Select(g => g.Industry).ToList();
            CustomerIndustryCounts = groupedCustomers.Select(g => g.Count).ToList();

            // ==== Biểu đồ 3: Doanh thu theo tháng ====
            MonthLabels = Enumerable.Range(1, 12).Select(m => $"Tháng {m}").ToList();
            MonthlyRevenue = Enumerable.Range(1, 12)
                .Select(m => deals
                    .Where(d => d.Deadline?.Month == m && d.Stage == "Closed Won")
                    .Sum(d => d.Value))
                .ToList();

            // ==== Biểu đồ 4: Win Rate theo tháng ====
            MonthlyWinRate = Enumerable.Range(1, 12)
                .Select(m =>
                {
                    var totalInMonth = deals.Where(d => d.Deadline?.Month == m).ToList();
                    var wonInMonth = totalInMonth.Where(d => d.Stage == "Closed Won").Count();
                    return totalInMonth.Count > 0
                        ? Math.Round((double)wonInMonth / totalInMonth.Count * 100, 2)
                        : 0;
                }).ToList();

            // ==== Biểu đồ 5: Phân loại Deal theo giá trị ====
            DealSizeCounts = new List<int>
            {
                deals.Count(d => d.Value < 10000000),
                deals.Count(d => d.Value >= 10000000 && d.Value <= 50000000),
                deals.Count(d => d.Value > 50000000)
            };

            // ==== Biểu đồ 6: Khách hàng mới theo tháng ====
            NewCustomersPerMonth = Enumerable.Range(1, 12)
                .Select(m => customers.Count(c => c.CreatedAt.Month == m))
                .ToList();
        }
    }
}
