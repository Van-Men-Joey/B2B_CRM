using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;

namespace Customer_Relationship_Management.Services.Implements
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IAuditLogService _auditLogService;

        public TaskService(ITaskRepository taskRepository, IAuditLogService auditLogService)
        {
            _taskRepository = taskRepository;
            _auditLogService = auditLogService;
        }

        public async System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId)
            => await _taskRepository.GetByEmployeeAsync(employeeId);

        public async System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3)
            => await _taskRepository.GetDueSoonAsync(employeeId, daysAhead);

        public async System.Threading.Tasks.Task<(bool Success, string Message)> CreateAsync(ModelTask task, int employeeId)
        {
            if (string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 200)
                return (false, "Tiêu ?? không h?p l?.");

            if (task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow.AddMinutes(-1))
                return (false, "Ngày gi? th?c hi?n không ???c ? quá kh?.");

            if (!string.IsNullOrEmpty(task.Status) && !new[] { "Pending", "In-Progress", "Done" }.Contains(task.Status, StringComparer.OrdinalIgnoreCase))
                return (false, "Tr?ng thái không h?p l?.");

            task.AssignedToUserID = employeeId;
            task.CreatedByUserID = employeeId;
            task.Status = task.Status ?? "Pending";
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            task.IsDeleted = false;

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Create, "Tasks", task.TaskID.ToString(), $"Create task: {task.Title}");
            return (true, "T?o Task thành công.");
        }

        public async System.Threading.Tasks.Task<(bool Success, string Message)> UpdateStatusAsync(int taskId, string status, int employeeId)
        {
            var valid = new[] { "Pending", "In-Progress", "Done" };
            if (!valid.Contains(status, StringComparer.OrdinalIgnoreCase))
                return (false, "Tr?ng thái không h?p l?.");

            var myTasks = await _taskRepository.GetByEmployeeAsync(employeeId);
            var task = myTasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task == null) return (false, "Không tìm th?y Task ho?c b?n không có quy?n.");

            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Update, "Tasks", task.TaskID.ToString(), $"Update status: {status}");
            return (true, "C?p nh?t tr?ng thái thành công.");
        }
    }
}
