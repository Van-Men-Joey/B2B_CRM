using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.Models
{
    public class SystemRestoreLog
    {
        [Key]
        public long RestoreID { get; set; }
        public int PerformedByUserID { get; set; }
        public string RestoreType { get; set; } = null!;
        public string? Target { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User PerformedByUser { get; set; } = null!;
    }
}
