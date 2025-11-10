using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Customer_Relationship_Management.Services.Implements
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            IAuditLogRepository auditLogRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async System.Threading.Tasks.Task LogAsync(
            int? userId,
            ActionType action,
            string tableName,
            string recordId,
            object? oldValue = null,
            object? newValue = null)
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            string ipAddress =
                httpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? httpContext?.Connection?.RemoteIpAddress?.ToString()
                ?? "Unknown";

            string? oldValueJson = oldValue == null ? null : AuditLog.ToJson(oldValue);
            string? newValueJson = newValue == null ? null : AuditLog.ToJson(newValue);

            var log = new AuditLog
            {
                UserID = userId,
                Action = action,
                TableName = tableName,
                RecordID = recordId,
                OldValue = oldValueJson,
                NewValue = newValueJson,
                IPAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(log);
            await _auditLogRepository.SaveChangesAsync();
        }
    }
}
