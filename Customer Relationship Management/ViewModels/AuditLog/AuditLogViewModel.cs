namespace Customer_Relationship_Management.ViewModels.AuditLog
{
    public class AuditLogViewModel
    {
        public long LogID { get; set; }
        public string Action { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public string RecordID { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IPAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Username { get; set; }
    }
}
