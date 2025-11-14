using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public interface ITaskService
    {
        // Employee scope
        Task<IEnumerable<ModelTask>> GetMyTasksAsync(int employeeId);
        Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3);
        Task<(bool Success, string Message)> CreateAsync(ModelTask task, int employeeId);
        Task<ModelTask?> GetByIdForEmployeeAsync(int taskId, int employeeId);
        Task<(bool Success, string Message)> UpdateAsync(ModelTask task, int employeeId);
        Task<(bool Success, string Message)> SoftDeleteAsync(int taskId, int employeeId);
        Task<(bool Success, string Message)> UpdateStatusAsync(int taskId, string status, int employeeId);

        // Manager scope (NEW, additive – không phá code cũ)
        Task<IEnumerable<ModelTask>> GetTeamTasksAsync(int managerUserId);
        Task<ModelTask?> GetByIdForManagerAsync(int taskId, int managerUserId);
        Task<(bool Success, string Message)> ManagerUpdateAsync(ModelTask task, int managerUserId);
        Task<(bool Success, string Message)> ManagerSoftDeleteAsync(int taskId, int managerUserId);
    }
}