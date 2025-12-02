using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Employee.Task
{
    [Authorize(Roles = "Employee,Manager")]
    public class TaskModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskModel> _logger;

        public TaskModel(ITaskService taskService, ILogger<TaskModel> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        public List<Models.Task> Tasks { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDue { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDue { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null)
                return RedirectToPage("/Account/Login");

            int employeeId = int.Parse(userIdClaim);

            var all = (await _taskService.GetMyTasksAsync(employeeId)).ToList();

            // Filter
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var k = Keyword.Trim().ToLower();
                all = all.Where(t => (t.Title ?? "").ToLower().Contains(k)
                    || (t.Description ?? "").ToLower().Contains(k)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(Status))
            {
                all = all.Where(t => string.Equals(t.Status, Status, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (FromDue.HasValue)
            {
                all = all.Where(t => t.DueDate != null && t.DueDate.Value >= FromDue.Value).ToList();
            }
            if (ToDue.HasValue)
            {
                all = all.Where(t => t.DueDate != null && t.DueDate.Value <= ToDue.Value).ToList();
            }
            // Sắp xếp hạn tăng dần
            Tasks = all.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
            return Page();
        }

        public async Task<JsonResult> OnGetTaskDetailAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null)
                return new JsonResult(null);

            int employeeId = int.Parse(userIdClaim);

            var myTasks = await _taskService.GetMyTasksAsync(employeeId);
            var task = myTasks.FirstOrDefault(t => t.TaskID == id);

            return new JsonResult(task);
        }

        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            var (ok, msg) = await _taskService.EmployeeUpdateTaskStatusAsync(id, employeeId, "In-Progress");
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            var (ok, msg) = await _taskService.EmployeeCompleteTaskAsync(id, employeeId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }
    }
}