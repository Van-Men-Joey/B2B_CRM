using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Pages.Admin
{
    // Đảm bảo chỉ Admin mới vào được trang này (nếu bạn đã có cấu hình Auth)
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IAdminService _adminService;
        private readonly IDealRepository _dealRepository;
        private readonly ICustomerRepository _customerRepository;

        public DashboardModel(
            IAdminService adminService,
            IDealRepository dealRepository,
            ICustomerRepository customerRepository)
        {
            _adminService = adminService;
            _dealRepository = dealRepository;
            _customerRepository = customerRepository;
        }

        // --- Properties hiển thị số liệu ---
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalDeals { get; set; }
        public decimal TotalRevenueExpectation { get; set; } // Tổng doanh thu dự kiến từ Deals

        // --- Properties cho danh sách hoạt động ---
        public IEnumerable<AuditLog> RecentActivities { get; set; }

        // --- Properties dữ liệu cho Biểu đồ (Chart.js) ---
        public string[] DealStageLabels { get; set; }
        public int[] DealStageValues { get; set; }

        public string[] CustomerIndustryLabels { get; set; }
        public int[] CustomerIndustryValues { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Lấy số liệu tổng quan từ AdminService
            var metrics = await _adminService.GetDashboardMetricsAsync();
            TotalUsers = metrics.TotalUsers;
            TotalCustomers = metrics.TotalCustomers;
            TotalDeals = metrics.TotalDeals;

            // 2. Lấy dữ liệu chi tiết Deals để tính doanh thu và vẽ biểu đồ
            var allDeals = await _dealRepository.GetAllAsync();

            // Tính tổng giá trị các Deal (giả sử model Deal có trường Amount hoặc Value, ở đây tôi giả định logic)
            // Nếu Deal không có trường Amount, bạn có thể bỏ dòng này.
            // TotalRevenueExpectation = allDeals.Sum(d => d.Amount ?? 0); 

            // Nhóm Deal theo Stage để vẽ biểu đồ
            var dealsByStage = allDeals
                .GroupBy(d => d.Stage)
                .Select(g => new { Stage = g.Key, Count = g.Count() })
                .ToList();

            DealStageLabels = dealsByStage.Select(x => x.Stage ?? "Không xác định").ToArray();
            DealStageValues = dealsByStage.Select(x => x.Count).ToArray();

            // 3. Lấy dữ liệu Customer để vẽ biểu đồ phân bố theo ngành (Industry)
            var allCustomers = await _customerRepository.GetAllAsync();
            var customersByIndustry = allCustomers
                .GroupBy(c => c.Industry)
                .Select(g => new { Industry = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5) // Lấy top 5 ngành nghề phổ biến nhất
                .ToList();

            CustomerIndustryLabels = customersByIndustry.Select(x => x.Industry ?? "Khác").ToArray();
            CustomerIndustryValues = customersByIndustry.Select(x => x.Count).ToArray();

            // 4. Lấy Nhật ký hoạt động (Audit Logs) 10 dòng mới nhất
            // Sử dụng tham số null để lấy tất cả, hoặc filter theo nhu cầu
            var logs = await _adminService.GetAuditLogsAsync(null, null, null);
            RecentActivities = logs.OrderByDescending(l => l.CreatedAt).Take(10).ToList();
        }
    }
}