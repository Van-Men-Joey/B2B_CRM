using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Customer_Relationship_Management.Repositories.Implements
{
    using ModelTask = Customer_Relationship_Management.Models.Task;

    public class TaskRepository : GenericRepository<ModelTask>, ITaskRepository
    {
        private readonly B2BDbContext _context;
        public TaskRepository(B2BDbContext context) : base(context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetByEmployeeAsync(int employeeId)
        {
            return await _context.Tasks
                .Include(t => t.RelatedDeal)
                .Where(t => !t.IsDeleted && t.AssignedToUserID == employeeId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async System.Threading.Tasks.Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3)
        {
            var now = DateTime.UtcNow;
            var until = now.AddDays(daysAhead);
            return await _context.Tasks
                .Where(t => !t.IsDeleted && t.AssignedToUserID == employeeId && t.DueDate != null && t.DueDate >= now && t.DueDate <= until)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public new async System.Threading.Tasks.Task UpdateAsync(ModelTask task)
        {
            _context.Tasks.Update(task);
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public new async System.Threading.Tasks.Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task SoftDeleteAsync(int taskId, int currentUserId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskID == taskId);
            if (task == null) return;

            if (task.AssignedToUserID != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa công việc này.");

            task.IsDeleted = true;
            task.UpdatedAt = DateTime.UtcNow;

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }
    }
}