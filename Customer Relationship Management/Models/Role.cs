using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }

        // Navigation: 1 Role – N Users
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
