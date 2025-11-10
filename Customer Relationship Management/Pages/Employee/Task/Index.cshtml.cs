using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Employee.Task
{
    [Authorize(Roles = "Employee,Manager")] // Manager có th? xem ?? assign
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITaskService taskService, ILogger<IndexModel> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        public List<Models.Task> Tasks { get; set; } = new();
        public List<Models.Task> DueSoon { get; set; } = new();

        [BindProperty] public Models.Task NewTask { get; set; } = new();
        [BindProperty] public int TaskIdToUpdate { get; set; }
        [BindProperty] public string? NewStatus { get; set; }

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            Tasks = (await _taskService.GetMyTasksAsync(employeeId)).ToList();
            DueSoon = (await _taskService.GetDueSoonAsync(employeeId, 5)).ToList();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> OnPostCreateAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "D? li?u không h?p l?.";
                return RedirectToPage();
            }

            var (ok, msg) = await _taskService.CreateAsync(NewTask, employeeId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> OnPostUpdateStatusAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            if (TaskIdToUpdate <= 0 || string.IsNullOrWhiteSpace(NewStatus))
            {
                TempData["ErrorMessage"] = "D? li?u c?p nh?t không h?p l?.";
                return RedirectToPage();
            }

            var (ok, msg) = await _taskService.UpdateStatusAsync(TaskIdToUpdate, NewStatus!, employeeId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }
    }
}
