using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Manager.Task
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IUserRepository _userRepo;

        public IndexModel(ITaskService taskService, IUserRepository userRepo)
        {
            _taskService = taskService;
            _userRepo = userRepo;
        }

        public List<Models.Task> Tasks { get; set; } = new();
        public List<Models.User> Team { get; set; } = new();

        // Filters
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public int? AssignedTo { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDue { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDue { get; set; }

        // Edit
        [BindProperty] public EditInput EditTask { get; set; } = new();
        public class EditInput
        {
            public int TaskID { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? ReminderAt { get; set; }
            public string? Status { get; set; } // Manager được đổi
        }

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
        {
            var managerId = GetManagerId();
            if (managerId == null) return RedirectToPage("/Account/Login");

            Team = (await _userRepo.GetEmployeesByManagerAsync(managerId.Value)).ToList();
            var list = (await _taskService.GetTeamTasksAsync(managerId.Value)).ToList();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var k = Keyword.Trim().ToLowerInvariant();
                list = list.Where(t => (t.Title ?? "").ToLower().Contains(k) || (t.Description ?? "").ToLower().Contains(k)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(Status))
                list = list.Where(t => string.Equals(t.Status, Status, StringComparison.OrdinalIgnoreCase)).ToList();
            if (AssignedTo.HasValue)
                list = list.Where(t => t.AssignedToUserID == AssignedTo.Value).ToList();
            if (FromDue.HasValue)
                list = list.Where(t => t.DueDate != null && t.DueDate.Value >= FromDue.Value).ToList();
            if (ToDue.HasValue)
                list = list.Where(t => t.DueDate != null && t.DueDate.Value <= ToDue.Value).ToList();

            Tasks = list.OrderByDescending(t => t.CreatedAt).ToList();
            return Page();
        }

        public async System.Threading.Tasks.Task<JsonResult> OnGetTaskDetailAsync(int id)
        {
            var managerId = GetManagerId();
            if (managerId == null) return new JsonResult(null);
            var t = await _taskService.GetByIdForManagerAsync(id, managerId.Value);
            if (t == null) return new JsonResult(null);
            return new JsonResult(new
            {
                t.TaskID,
                t.Title,
                t.Description,
                t.Status,
                t.DueDate,
                t.ReminderAt,
                t.CreatedAt,
                t.UpdatedAt,
                t.AssignedToUserID,
                AssignedName = t.AssignedToUser?.FullName
            });
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostEditAsync()
        {
            var managerId = GetManagerId();
            if (managerId == null) return RedirectToPage("/Account/Login");

            var toUpdate = new Models.Task
            {
                TaskID = EditTask.TaskID,
                Title = EditTask.Title?.Trim() ?? "",
                Description = string.IsNullOrWhiteSpace(EditTask.Description) ? null : EditTask.Description.Trim(),
                DueDate = EditTask.DueDate,
                ReminderAt = EditTask.ReminderAt,
                Status = EditTask.Status
            };

            var (ok, msg) = await _taskService.ManagerUpdateAsync(toUpdate, managerId.Value);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage(new { Keyword, Status, AssignedTo, FromDue, ToDue });
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var managerId = GetManagerId();
            if (managerId == null) return RedirectToPage("/Account/Login");
            var (ok, msg) = await _taskService.ManagerSoftDeleteAsync(id, managerId.Value);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage(new { Keyword, Status, AssignedTo, FromDue, ToDue });
        }

        private int? GetManagerId()
        {
            var v = User.FindFirst("UserID")?.Value;
            return int.TryParse(v, out var id) ? id : (int?)null;
        }
    }
}