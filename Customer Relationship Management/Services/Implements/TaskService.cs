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
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService; // Stub notification

        public TaskService(
            ITaskRepository taskRepository,
            IAuditLogService auditLogService,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _taskRepository = taskRepository;
            _auditLogService = auditLogService;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        // EMPLOYEE
        public async Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId)
            => await _taskRepository.GetByEmployeeAsync(employeeId);

        public async Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 5)
            => await _taskRepository.GetDueSoonAsync(employeeId, daysAhead);

        public async Task<(bool Success, string Message)> EmployeeCompleteTaskAsync(int taskId, int employeeId)
        {
            var myTasks = await _taskRepository.GetByEmployeeAsync(employeeId);
            var task = myTasks.FirstOrDefault(t => t.TaskID == taskId && t.Status != "Done" && !t.IsDeleted);
            if (task == null)
                return (false, "Không tìm thấy công việc hoặc đã hoàn thành/xóa.");

            task.Status = "Done";
            task.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Update, "Tasks", task.TaskID.ToString(),
                $"Hoàn thành Task: {task.Title}");

            return (true, "Đã hoàn thành công việc!");
        }
        public async Task<(bool Success, string Message)> EmployeeUpdateTaskStatusAsync(int taskId, int employeeId, string newStatus)
        {
            var myTasks = await _taskRepository.GetByEmployeeAsync(employeeId);
            var task = myTasks.FirstOrDefault(t => t.TaskID == taskId && t.Status == "Pending" && !t.IsDeleted);

            if (task == null)
                return (false, "Không tìm thấy công việc hoặc đã chuyển trạng thái.");

            if (newStatus != "In-Progress")
                return (false, "Trạng thái cập nhật không hợp lệ.");

            task.Status = "In-Progress";
            task.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(employeeId, ActionType.Update, "Tasks", task.TaskID.ToString(),
                $"Chuyển task sang trạng thái In-Progress: {task.Title}");

            return (true, "Đã xác nhận bắt đầu công việc!");
        }

        // MANAGER
        public async Task<IEnumerable<ModelTask>> GetTeamTasksAsync(int managerUserId)
        {
            var employees = await _userRepository.GetEmployeesByManagerAsync(managerUserId);
            var employeeIds = employees.Select(e => e.UserID).ToList();

            var allTasks = new List<ModelTask>();
            foreach (var empId in employeeIds)
            {
                var tasks = await _taskRepository.GetByEmployeeAsync(empId);
                allTasks.AddRange(tasks);
            }
            return allTasks.Where(t => !t.IsDeleted).OrderByDescending(t => t.CreatedAt).ToList();
        }

        public async Task<(bool Success, string Message)> ManagerAssignTaskAsync(
            ModelTask task,
            IEnumerable<int> employeeIds,
            int managerUserId)
        {
            var team = await _userRepository.GetEmployeesByManagerAsync(managerUserId);
            var validTeamIds = team.Select(u => u.UserID).ToHashSet();
            var targetEmployeeIds = employeeIds.Distinct().Where(id => validTeamIds.Contains(id)).ToList();

            if (targetEmployeeIds.Count == 0)
                return (false, "Không có nhân viên hợp lệ để giao.");

            if (string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 200)
                return (false, "Tiêu đề không hợp lệ (1-200 ký tự).");

            if (task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow.AddMinutes(-1))
                return (false, "Ngày hạn không được ở quá khứ.");

            var createdTasks = new List<ModelTask>();
            foreach (var employeeId in targetEmployeeIds)
            {
                var newTask = new ModelTask
                {
                    Title = task.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(task.Description) ? null : task.Description.Trim(),
                    DueDate = task.DueDate,
                    ReminderAt = task.ReminderAt,
                    Status = "Pending",
                    AssignedToUserID = employeeId,
                    CreatedByUserID = managerUserId,
                    RelatedDealID = task.RelatedDealID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _taskRepository.AddAsync(newTask);
                createdTasks.Add(newTask);

                // Gửi thông báo nhắc nhở nếu ReminderAt >= Now
                if (newTask.ReminderAt.HasValue && newTask.ReminderAt.Value > DateTime.UtcNow)
                {
                    await _notificationService.NotifyTaskAssignedAsync(employeeId, newTask.TaskID, task.Title, newTask.ReminderAt.Value);
                }
            }

            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(managerUserId, ActionType.Create, "Tasks", "-",
                $"Giao {createdTasks.Count} công việc '{task.Title}' cho nhân viên");

            return (true, $"Giao thành công cho {createdTasks.Count} nhân viên.");
        }

        // Gửi nhắc lại (bổ sung)
        public async Task<(bool Success, string Message)> ManagerRemindTaskAsync(int taskId, int managerUserId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null || task.IsDeleted || task.Status == "Done")
                return (false, "Không thể nhắc nhở task này.");

            await _notificationService.NotifyTaskAssignedAsync(task.AssignedToUserID, task.TaskID, task.Title, DateTime.UtcNow);
            await _auditLogService.LogAsync(managerUserId, ActionType.Update, "Tasks", task.TaskID.ToString(),
                $"Manager nhắc lại Task: {task.Title}");
            return (true, "Đã gửi nhắc nhở cho nhân viên.");
        }

        public async Task<(bool Success, string Message)> ManagerEditTaskAsync(ModelTask updateData, int managerUserId)
        {
            var task = await _taskRepository.GetByIdAsync(updateData.TaskID);
            if (task == null || task.IsDeleted)
                return (false, "Task không tồn tại hoặc đã bị xóa.");

            var assignedEmployee = await _userRepository.GetByIdAsync(task.AssignedToUserID);
            if (assignedEmployee == null || assignedEmployee.ManagerID != managerUserId)
                return (false, "Task không thuộc team của bạn.");

            if (string.IsNullOrWhiteSpace(updateData.Title) || updateData.Title.Length > 200)
                return (false, "Tiêu đề không hợp lệ (1-200 ký tự).");

            if (updateData.DueDate.HasValue && updateData.DueDate.Value < DateTime.UtcNow.AddMinutes(-1))
                return (false, "Ngày hạn không được ở quá khứ.");

            if (!string.IsNullOrWhiteSpace(updateData.Status) &&
                !new[] { "Pending", "In-Progress", "Done" }.Contains(updateData.Status, StringComparer.OrdinalIgnoreCase))
                return (false, "Trạng thái không hợp lệ.");

            task.Title = updateData.Title.Trim();
            task.Description = string.IsNullOrWhiteSpace(updateData.Description) ? null : updateData.Description.Trim();
            task.DueDate = updateData.DueDate;
            task.ReminderAt = updateData.ReminderAt;
            if (!string.IsNullOrWhiteSpace(updateData.Status))
                task.Status = updateData.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(managerUserId, ActionType.Update, "Tasks", task.TaskID.ToString(),
                $"Chỉnh sửa Task: {task.Title}");

            return (true, "Cập nhật Task thành công.");
        }

        public async Task<(bool Success, string Message)> ManagerDeleteTaskAsync(int taskId, int managerUserId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null || task.IsDeleted)
                return (false, "Task không tồn tại hoặc đã bị xóa.");

            var assignedEmployee = await _userRepository.GetByIdAsync(task.AssignedToUserID);
            if (assignedEmployee == null || assignedEmployee.ManagerID != managerUserId)
                return (false, "Task không thuộc team của bạn.");

            task.IsDeleted = true;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(managerUserId, ActionType.Delete, "Tasks", taskId.ToString(),
                $"Xóa Task: {task.Title}");

            return (true, "Xóa Task thành công.");
        }
    }
}