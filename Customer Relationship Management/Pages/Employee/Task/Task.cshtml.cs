using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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
        public List<Models.Task> DueSoon { get; set; } = new();

        // Filters
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDue { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDue { get; set; }

        // Add form
        [BindProperty] public AddTaskInputModel AddTask { get; set; } = new();
        public class AddTaskInputModel
        {
            [Required(ErrorMessage = "Tiêu đề không được để trống.")]
            [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
            public string Title { get; set; } = string.Empty;

            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? ReminderAt { get; set; }

            // Không cho chọn trạng thái khi tạo (luôn Pending)
            public string? Status { get; set; }
        }

        // Edit form
        [BindProperty] public EditTaskInputModel EditTask { get; set; } = new();
        public class EditTaskInputModel
        {
            public int TaskID { get; set; }

            [Required(ErrorMessage = "Tiêu đề không được để trống.")]
            [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
            public string Title { get; set; } = string.Empty;

            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? ReminderAt { get; set; }

            // Nhân viên không được đổi trạng thái
            public string? Status { get; set; }
        }

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            var all = (await _taskService.GetMyTasksAsync(employeeId)).ToList();
            DueSoon = (await _taskService.GetDueSoonAsync(employeeId, 5)).ToList();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var k = Keyword.Trim().ToLower();
                all = all.Where(t => (t.Title ?? "").ToLower().Contains(k) || (t.Description ?? "").ToLower().Contains(k)).ToList();
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

            Tasks = all.OrderByDescending(t => t.CreatedAt).ToList();
            return Page();
        }

        // JSON detail
        public async System.Threading.Tasks.Task<JsonResult> OnGetTaskDetailAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return new JsonResult(null);
            int employeeId = int.Parse(userIdClaim);

            var task = await _taskService.GetByIdForEmployeeAsync(id, employeeId);
            return new JsonResult(task);
        }

        // Add – ép Pending
        public async System.Threading.Tasks.Task<IActionResult> OnPostAddAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            ModelState.Clear();
            if (!TryValidateModel(AddTask, nameof(AddTask)))
            {
                TempData["ErrorMessage"] = string.Join("; ",
                    ModelState.Where(kvp => kvp.Key.StartsWith($"{nameof(AddTask)}.", StringComparison.OrdinalIgnoreCase))
                              .SelectMany(v => v.Value!.Errors).Select(e => e.ErrorMessage).Distinct());
                return await OnGetAsync();
            }

            var entity = new Models.Task
            {
                Title = AddTask.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(AddTask.Description) ? null : AddTask.Description.Trim(),
                DueDate = AddTask.DueDate,
                ReminderAt = AddTask.ReminderAt,
                Status = "Pending" // ÉP pending
            };

            var (ok, msg) = await _taskService.CreateAsync(entity, employeeId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }

        // Edit – nhân viên không đổi trạng thái
        public async System.Threading.Tasks.Task<IActionResult> OnPostEditAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            ModelState.Clear();
            if (!TryValidateModel(EditTask, nameof(EditTask)))
            {
                TempData["ErrorMessage"] = string.Join("; ",
                    ModelState.Where(kvp => kvp.Key.StartsWith($"{nameof(EditTask)}.", StringComparison.OrdinalIgnoreCase))
                              .SelectMany(v => v.Value!.Errors).Select(e => e.ErrorMessage).Distinct());
                return RedirectToPage();
            }

            var current = await _taskService.GetByIdForEmployeeAsync(EditTask.TaskID, employeeId);
            if (current == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Task hoặc bạn không có quyền.";
                return RedirectToPage();
            }

            bool changed =
                current.Title != EditTask.Title ||
                (current.Description ?? "") != (EditTask.Description ?? "") ||
                current.DueDate != EditTask.DueDate ||
                current.ReminderAt != EditTask.ReminderAt;

            if (!changed)
            {
                TempData["InfoMessage"] = "Không có thay đổi để cập nhật.";
                return RedirectToPage();
            }

            var toUpdate = new Models.Task
            {
                TaskID = EditTask.TaskID,
                Title = EditTask.Title,
                Description = EditTask.Description,
                DueDate = EditTask.DueDate,
                ReminderAt = EditTask.ReminderAt,
                Status = null // KHÔNG cập nhật trạng thái từ employee
            };

            var (ok, msg) = await _taskService.UpdateAsync(toUpdate, employeeId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }

        // Delete
        public async System.Threading.Tasks.Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            var (ok, msg) = await _taskService.SoftDeleteAsync(id, employeeId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToPage();
        }
    }
}