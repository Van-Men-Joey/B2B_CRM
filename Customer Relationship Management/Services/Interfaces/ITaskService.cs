using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public interface ITaskService
    {
        Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId);
        Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3);

        // CRUD đầy đủ
        Task<(bool Success, string Message)> CreateAsync(ModelTask task, int employeeId);
        Task<ModelTask?> GetByIdForEmployeeAsync(int taskId, int employeeId);
        Task<(bool Success, string Message)> UpdateAsync(ModelTask task, int employeeId);
        Task<(bool Success, string Message)> SoftDeleteAsync(int taskId, int employeeId);

        // Cập nhật nhanh trạng thái
        Task<(bool Success, string Message)> UpdateStatusAsync(int taskId, string status, int employeeId);
    }
}