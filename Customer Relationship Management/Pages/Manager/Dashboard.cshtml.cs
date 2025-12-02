using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Customer_Relationship_Management.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
// Thêm using này để giải quyết lỗi 'Task' là namespace (nếu bạn sử dụng model Task)
using ModelTask = Customer_Relationship_Management.Models.Task;

namespace Customer_Relationship_Management.Pages.Manager
{
    public class DashboardModel : PageModel
    {
        private readonly ICustomerService _customerService;
        private readonly IContractService _contractService;
        private readonly IDealService _dealService;
        private readonly ITaskService _taskService;
        private readonly IUserService _userService;

        public DashboardModel(
            ICustomerService customerService,
            IContractService contractService,
            IDealService dealService,
            ITaskService taskService,
            IUserService userService)
        {
            _customerService = customerService;
            _contractService = contractService;
            _dealService = dealService;
            _taskService = taskService;
            _userService = userService;
        }

        // --- Thuộc tính Model giữ nguyên ---
        public int TotalCustomers { get; set; }
        public int PendingContracts { get; set; }
        public int TasksDueToday { get; set; }
        public int OpenDealsCount { get; set; }
        public Dictionary<string, decimal> PipelineSummary { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();

        // **SỬA LỖI 1, 4**: Đổi kiểu trả về thành Task<IActionResult> và đảm bảo tất cả nhánh code trả về giá trị
        public async Task<IActionResult> OnGetAsync()
        {
            var managerId = GetCurrentUserId();
            // **SỬA LỖI 1**: Nếu không tìm thấy ID, trả về trang
            if (managerId == 0) return RedirectToPage("/Account/Login");

            // 1. Lấy danh sách ID của team (bao gồm cả Manager)
            var employees = await _userService.GetEmployeesByManagerAsync(managerId);
            var teamUserIds = employees.Select(e => e.UserID).Append(managerId).ToList();

            // 2. Thống kê nhanh
            var teamCustomers = await _customerService.GetCustomersForTeamAsync(teamUserIds);
            TotalCustomers = teamCustomers.Count();

            var allContracts = await _contractService.GetByManagerAsync(managerId);
            PendingContracts = allContracts.Count(c => c.ApprovalStatus == "Pending" && c.IsDeleted == false);

            var openStages = new[] { "Lead", "Negotiation", "Contract Sent" };
            var teamDeals = await _dealService.GetTeamDealsAsync(managerId);
            OpenDealsCount = teamDeals.Count(d => openStages.Contains(d.Stage) && d.IsDeleted == false);

            var teamTasks = await _taskService.GetTeamTasksAsync(managerId);
            TasksDueToday = teamTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today.Date && t.Status != "Done");

            // 3. **SỬA LỖI 3**: Dùng .ToDictionary() để chuyển từ IDictionary sang Dictionary
            PipelineSummary = (await _dealService.GetTeamPipelineSummaryAsync(managerId))
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 4. Hoạt động gần đây (Mock data)
            RecentActivities = new List<ActivityLog>
            {
                new ActivityLog { Icon = "bi-check2-circle", ColorClass = "text-success", Message = $"Phê duyệt {PendingContracts} Hợp đồng chờ", Time = "Vừa xong" },
                new ActivityLog { Icon = "bi-briefcase-fill", ColorClass = "text-info", Message = $"Có {OpenDealsCount} Deal đang được tiến hành", Time = "Hôm nay" },
                new ActivityLog { Icon = "bi-person-plus", ColorClass = "text-primary", Message = $"Gán lại khách hàng cho nhân viên", Time = "1 giờ trước" },
                new ActivityLog { Icon = "bi-calendar-x", ColorClass = "text-danger", Message = $"Có {TasksDueToday} Task cần hoàn thành hôm nay", Time = "Sáng nay" }
            };

            return Page(); // **SỬA LỖI 1**: Giá trị trả về ở cuối
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst("UserID");
            return idClaim != null && int.TryParse(idClaim.Value, out var id) ? id : 0;
        }

        public class ActivityLog
        {
            public string Icon { get; set; } = string.Empty;
            public string ColorClass { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Time { get; set; } = string.Empty;
        }
    }
}