using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Services.Interfaces
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public interface ITaskService
    {
        System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId);
        System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3);
        System.Threading.Tasks.Task<(bool Success, string Message)> CreateAsync(ModelTask task, int employeeId);
        System.Threading.Tasks.Task<(bool Success, string Message)> UpdateStatusAsync(int taskId, string status, int employeeId);
    }
}
