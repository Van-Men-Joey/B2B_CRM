using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public interface ITaskRepository : IGenericRepository<ModelTask>
    {
        Task<IEnumerable<ModelTask>> GetByEmployeeAsync(int employeeId);
        Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3);
        Task UpdateAsync(ModelTask task);
        Task SaveChangesAsync();
        Task SoftDeleteAsync(int taskId, int currentUserId);

        // NEW: cho Manager (truy vấn theo team)
        Task<IEnumerable<ModelTask>> GetByAssignedUserIdsAsync(IEnumerable<int> userIds);
        Task<ModelTask?> GetByIdForAssignedUserIdsAsync(int taskId, IEnumerable<int> userIds);
        Task SoftDeleteByManagerAsync(int taskId, IEnumerable<int> teamUserIds);
    }
}