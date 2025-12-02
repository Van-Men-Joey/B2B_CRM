using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.Pages.Manager.Task
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITaskService taskService, IUserRepository userRepo, ILogger<IndexModel> logger)
        {
            _taskService = taskService;
            _userRepo = userRepo;
            _logger = logger;
        }

        public List<Models.Task> Tasks { get; set; } = new();
        public List<Models.User> TeamMembers { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public int? AssignedTo { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDue { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDue { get; set; }

        [BindProperty] public AssignInput AssignTask { get; set; } = new();

        public class AssignInput
        {
            [Required(ErrorMessage = "Tiêu đề không được để trống")]
            [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
            public string Title { get; set; } = "";

            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? ReminderAt { get; set; }
            [Required(ErrorMessage = "Vui lòng chọn ít nhất một nhân viên")]
            public int[] EmployeeIds { get; set; } = Array.Empty<int>();
        }

        [BindProperty] public EditInput EditTask { get; set; } = new();

        public class EditInput
        {
            public int TaskID { get; set; }
            public int AssignedToUserID { get; set; }
            [Required(ErrorMessage = "Tiêu đề không được để trống")]
            [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? ReminderAt { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var managerId = GetManagerId();
            if (managerId == null)
                return RedirectToPage("/Account/Login");

            TeamMembers = (await _userRepo.GetEmployeesByManagerAsync(managerId.Value))
                .OrderBy(u => u.FullName)
                .ToList();

            var list = (await _taskService.GetTeamTasksAsync(managerId.Value)).ToList();

            // FILTER
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var k = Keyword.Trim().ToLowerInvariant();
                list = list.Where(t => (t.Title ?? "").ToLower().Contains(k)
                    || (t.Description ?? "").ToLower().Contains(k))
                    .ToList();
            }
            if (!string.IsNullOrWhiteSpace(Status))
            {
                list = list.Where(t => string.Equals(t.Status, Status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (AssignedTo.HasValue)
            {
                list = list.Where(t => t.AssignedToUserID == AssignedTo.Value)
                    .ToList();
            }
            if (FromDue.HasValue)
            {
                list = list.Where(t => t.DueDate != null && t.DueDate.Value >= FromDue.Value)
                    .ToList();
            }
            if (ToDue.HasValue)
            {
                list = list.Where(t => t.DueDate != null && t.DueDate.Value <= ToDue.Value)
                    .ToList();
            }

            // SẮP ĐẶT THEO DUE DATE TĂNG DẦN (công việc gần hạn lên đầu)
            Tasks = list.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
            return Page();
        }

        public async Task<JsonResult> OnGetTaskDetailAsync(int id)
        {
            var managerId = GetManagerId();
            if (managerId == null)
                return new JsonResult(null);

            var allTasks = await _taskService.GetTeamTasksAsync(managerId.Value);
            var task = allTasks.FirstOrDefault(t => t.TaskID == id && !t.IsDeleted);

            if (task == null)
                return new JsonResult(null);

            return new JsonResult(new
            {
                task.TaskID,
                task.Title,
                task.Description,
                task.Status,
                task.DueDate,
                task.ReminderAt,
                task.CreatedAt,
                task.UpdatedAt,
                task.AssignedToUserID,
                AssignedName = task.AssignedToUser?.FullName,
                task.IsDeleted
            });
        }

        // GIAO TASK CHO NHIỀU NHÂN VIÊN
        public async Task<IActionResult> OnPostAssignAsync()
        {
            var managerId = GetManagerId();
            if (managerId == null)
                return RedirectToPage("/Account/Login");

            if (string.IsNullOrWhiteSpace(AssignTask.Title) || AssignTask.EmployeeIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tiêu đề và chọn ít nhất một nhân viên.";
                return RedirectToPage();
            }

            // Chỉ lấy các employeeId thuộc team manager
            var validTeamIds = (await _userRepo.GetEmployeesByManagerAsync(managerId.Value)).Select(x => x.UserID).ToHashSet();
            var filteredEmployeeIds = AssignTask.EmployeeIds.Where(id => validTeamIds.Contains(id)).ToArray();
            if (filteredEmployeeIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Nhân viên không hợp lệ!";
                return RedirectToPage();
            }

            var task = new Models.Task
            {
                Title = AssignTask.Title,
                Description = AssignTask.Description,
                DueDate = AssignTask.DueDate,
                ReminderAt = AssignTask.ReminderAt,
            };

            var (ok, msg) = await _taskService.ManagerAssignTaskAsync(task, filteredEmployeeIds, managerId.Value);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;

            return RedirectToPage();
        }

        // SỬA TASK: chỉ chọn lại AssignedToUserID (dropdown), còn lại giữ đúng trạng thái gốc
        public async Task<IActionResult> OnPostEditAsync()
        {
            var managerId = GetManagerId();
            if (managerId == null)
                return RedirectToPage("/Account/Login");

            var allTasks = await _taskService.GetTeamTasksAsync(managerId.Value);
            var existingTask = allTasks.FirstOrDefault(t => t.TaskID == EditTask.TaskID);
            if (existingTask == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy task!";
                return RedirectToPage();
            }

            // Validate nhân viên thuộc team
            var teamIds = (await _userRepo.GetEmployeesByManagerAsync(managerId.Value)).Select(x => x.UserID).ToHashSet();
            if (!teamIds.Contains(EditTask.AssignedToUserID))
            {
                TempData["ErrorMessage"] = "Nhân viên không thuộc quyền quản lý!";
                return RedirectToPage();
            }

            var toUpdate = new Models.Task
            {
                TaskID = EditTask.TaskID,
                Title = EditTask.Title?.Trim() ?? "",
                Description = string.IsNullOrWhiteSpace(EditTask.Description) ? null : EditTask.Description.Trim(),
                DueDate = EditTask.DueDate,
                ReminderAt = EditTask.ReminderAt,
                Status = existingTask.Status,
                AssignedToUserID = EditTask.AssignedToUserID
            };

            var (ok, msg) = await _taskService.ManagerEditTaskAsync(toUpdate, managerId.Value);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;

            return RedirectToPage(new { Keyword, Status, AssignedTo, FromDue, ToDue });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var managerId = GetManagerId();
            if (managerId == null)
                return RedirectToPage("/Account/Login");

            var (ok, msg) = await _taskService.ManagerDeleteTaskAsync(id, managerId.Value);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;

            return RedirectToPage(new { Keyword, Status, AssignedTo, FromDue, ToDue });
        }

        public async Task<IActionResult> OnPostRemindAsync(int id)
        {
            var managerId = GetManagerId();
            if (managerId == null)
                return RedirectToPage("/Account/Login");

            var (ok, msg) = await _taskService.ManagerRemindTaskAsync(id, managerId.Value);
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