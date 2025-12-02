using System;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyTaskAssignedAsync(int employeeId, int taskId, string title, DateTime reminderAt);
    }
}