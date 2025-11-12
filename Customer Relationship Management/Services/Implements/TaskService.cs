using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId)
            => await _taskRepository.GetByEmployeeAsync(employeeId);

        public async Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3)
            => await _taskRepository.GetDueSoonAsync(employeeId, daysAhead);

        public async Task<(bool Success, string Message)> CreateAsync(ModelTask task, int employeeId)
        {
            if (string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 200)
                return (false, "Tiêu đề không hợp lệ.");

            if (task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow.AddMinutes(-1))
                return (false, "Ngày giờ thực hiện không được ở quá khứ.");

            if (!string.IsNullOrEmpty(task.Status) &&
                !new[] { "Pending", "In-Progress", "Done" }.Contains(task.Status, StringComparer.OrdinalIgnoreCase))
                return (false, "Trạng thái không hợp lệ.");

            task.AssignedToUserID = employeeId;
            task.CreatedByUserID = employeeId;
            task.Status = string.IsNullOrWhiteSpace(task.Status) ? "Pending" : task.Status;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            task.IsDeleted = false;

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Create, "Tasks", task.TaskID.ToString(), $"Create task: {task.Title}");
            return (true, "Tạo Task thành công.");
        }

        public async Task<ModelTask?> GetByIdForEmployeeAsync(int taskId, int employeeId)
        {
            var myTasks = await _taskRepository.GetByEmployeeAsync(employeeId);
            return myTasks.FirstOrDefault(t => t.TaskID == taskId);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(ModelTask task, int employeeId)
        {
            var current = await GetByIdForEmployeeAsync(task.TaskID, employeeId);
            if (current == null)
                return (false, "Không tìm thấy Task hoặc bạn không có quyền.");

            if (string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 200)
                return (false, "Tiêu đề không hợp lệ.");

            if (task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow.AddMinutes(-1))
                return (false, "Ngày giờ thực hiện không được ở quá khứ.");

            if (!string.IsNullOrEmpty(task.Status) &&
                !new[] { "Pending", "In-Progress", "Done" }.Contains(task.Status, StringComparer.OrdinalIgnoreCase))
                return (false, "Trạng thái không hợp lệ.");

            current.Title = task.Title.Trim();
            current.Description = string.IsNullOrWhiteSpace(task.Description) ? null : task.Description.Trim();
            current.DueDate = task.DueDate;
            current.ReminderAt = task.ReminderAt;
            if (!string.IsNullOrWhiteSpace(task.Status))
                current.Status = task.Status;
            current.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(current);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Update, "Tasks", current.TaskID.ToString(), $"Update task: {current.Title}");
            return (true, "Cập nhật Task thành công.");
        }

        public async Task<(bool Success, string Message)> SoftDeleteAsync(int taskId, int employeeId)
        {
            try
            {
                await _taskRepository.SoftDeleteAsync(taskId, employeeId);
                await _auditLogService.LogAsync(employeeId, ActionType.Delete, "Tasks", taskId.ToString(), "Soft delete task");
                return (true, "Xóa Task thành công.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, ex.Message);
            }
            catch
            {
                return (false, "Lỗi khi xóa Task.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(int taskId, string status, int employeeId)
        {
            var valid = new[] { "Pending", "In-Progress", "Done" };
            if (!valid.Contains(status, StringComparer.OrdinalIgnoreCase))
                return (false, "Trạng thái không hợp lệ.");

            var myTasks = await _taskRepository.GetByEmployeeAsync(employeeId);
            var task = myTasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task == null) return (false, "Không tìm thấy Task hoặc bạn không có quyền.");

            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Update, "Tasks", task.TaskID.ToString(), $"Update status: {status}");
            return (true, "Cập nhật trạng thái thành công.");
        }
    }
}