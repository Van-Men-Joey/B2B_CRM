namespace Customer_Relationship_Management.Services.Interfaces
{
    using Customer_Relationship_Management.Models;

    public interface IAuditLogService
    {
        System.Threading.Tasks.Task LogAsync(int? userId, ActionType action, string tableName, string recordId, object? oldValue = null, object? newValue = null);
    }
}
