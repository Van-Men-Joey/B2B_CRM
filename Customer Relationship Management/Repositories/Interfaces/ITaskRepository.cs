using Customer_Relationship_Management.Models;
using System.Collections.Generic;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public interface ITaskRepository : IGenericRepository<ModelTask>
    {
        System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetByEmployeeAsync(int employeeId);
        System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3);
        System.Threading.Tasks.Task UpdateAsync(ModelTask task);
        System.Threading.Tasks.Task SaveChangesAsync();
    }
}
