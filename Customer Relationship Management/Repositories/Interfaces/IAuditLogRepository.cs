using Customer_Relationship_Management.Models;
using System.Collections.Generic;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        System.Threading.Tasks.Task AddAsync(AuditLog log);
        System.Threading.Tasks.Task SaveChangesAsync();

        // Tìm log theo điều kiện (lọc theo bảng, user, hành động)
        System.Threading.Tasks.Task<IEnumerable<AuditLog>> FindAsync(
            string? tableName = null,
            int? userId = null,
            ActionType? action = null
        );
    }
}
