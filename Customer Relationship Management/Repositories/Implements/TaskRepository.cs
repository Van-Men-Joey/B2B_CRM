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

        public async Task<IEnumerable<ModelTask>> GetByEmployeeAsync(int employeeId)
        {
            return await _context.Tasks
                .Include(t => t.RelatedDeal)
                .Include(t => t.AssignedToUser)
                .Where(t => !t.IsDeleted && t.AssignedToUserID == employeeId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ModelTask>> GetDueSoonAsync(int employeeId, int daysAhead = 3)
        {
            var now = DateTime.UtcNow;
            var until = now.AddDays(daysAhead);

            return await _context.Tasks
                .Where(t => !t.IsDeleted
                    && t.AssignedToUserID == employeeId
                    && t.DueDate.HasValue
                    && t.DueDate >= now
                    && t.DueDate <= until)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<ModelTask?> GetByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.RelatedDeal)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && !t.IsDeleted);
        }

        public async Task AddAsync(ModelTask task)
        {
            await _context.Tasks.AddAsync(task);
        }

        public async Task UpdateAsync(ModelTask task)
        {
            _context.Tasks.Update(task);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}