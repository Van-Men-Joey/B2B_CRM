using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Customer_Relationship_Management.Models
{
    public enum ActionType
    {
        Create,
        Update,
        Delete,
        Login,
        Logout,
        Restore,
        Error
    }

    public class AuditLog
    {
        [Key]
        public long LogID { get; set; }

        public int? UserID { get; set; } // ai thực hiện

        public ActionType Action { get; set; } // Enum Action

        public string TableName { get; set; } = null!; // bảng bị ảnh hưởng

        public string RecordID { get; set; } = null!; // ID record

        public string? OldValue { get; set; } // JSON

        public string? NewValue { get; set; } // JSON

        public string? IPAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserID")]
        public User? User { get; set; }

        // Helper method: convert object to JSON string
        public static string ToJson(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                  ReferenceHandler = ReferenceHandler.IgnoreCycles, // ✅ Bỏ qua vòng lặp
            });
        }
    }
}
