using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public interface ITaskService
    {
        // EMPLOYEE
        Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId); // Danh sách task được giao cho nhân viên
        Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 5); // Task sắp tới hạn
        Task<(bool Success, string Message)> EmployeeCompleteTaskAsync(int taskId, int employeeId); // Hoàn thành task cá nhân
        Task<(bool Success, string Message)> EmployeeUpdateTaskStatusAsync(int taskId, int employeeId, string newStatus);

        // MANAGER
        Task<IEnumerable<ModelTask>> GetTeamTasksAsync(int managerUserId); // Tất cả task team
        Task<(bool Success, string Message)> ManagerAssignTaskAsync(ModelTask task, IEnumerable<int> employeeIds, int managerUserId); // Giao task cho nhiều nhân viên
        Task<(bool Success, string Message)> ManagerRemindTaskAsync(int taskId, int managerUserId); // Gửi nhắc nhở lại
        // Xóa/sửa task nếu cần thiết
        Task<(bool Success, string Message)> ManagerEditTaskAsync(ModelTask updateData, int managerUserId);
        Task<(bool Success, string Message)> ManagerDeleteTaskAsync(int taskId, int managerUserId);
    }
}