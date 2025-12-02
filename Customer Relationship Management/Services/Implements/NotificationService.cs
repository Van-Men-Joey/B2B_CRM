using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Customer_Relationship_Management.Services.Implements
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public Task NotifyTaskAssignedAsync(int employeeId, int taskId, string title, DateTime reminderAt)
        {
            // Thực tế có thể gửi email, push notification...
            _logger.LogInformation($"[NOTIFY] Nhân viên {employeeId} nhận task '{title}' (TaskID={taskId}) - Reminder at {reminderAt}");
            return Task.CompletedTask;
        }
    }
}