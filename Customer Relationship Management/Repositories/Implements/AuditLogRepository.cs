using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Customer_Relationship_Management.Repositories.Implements
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly B2BDbContext _context;

        public AuditLogRepository(B2BDbContext context)
        {
            _context = context;
        }

        // 🧩 Thêm log mới
        public async System.Threading.Tasks.Task AddAsync(AuditLog log)
        {
            await _context.AuditLogs.AddAsync(log);
        }

        // 💾 Lưu thay đổi
        public async System.Threading.Tasks.Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // 🔍 Tìm log theo điều kiện
        public async System.Threading.Tasks.Task<IEnumerable<AuditLog>> FindAsync(
            string? tableName = null,
            int? userId = null,
            ActionType? action = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(l => l.TableName == tableName);

            if (userId.HasValue)
                query = query.Where(l => l.UserID == userId.Value);

            if (action.HasValue)
                query = query.Where(l => l.Action == action.Value);

            return await query
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
    }
}
