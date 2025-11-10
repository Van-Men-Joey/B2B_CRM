using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Customer_Relationship_Management.Repositories.Interfaces;

namespace Customer_Relationship_Management.Models
{
    public class Customer : IEntity
    {
        [Key]
        public int CustomerID { get; set; }
        [NotMapped]
        public int Id => CustomerID; // ánh xạ về Id chung
        public string? CustomerCode { get; set; } // CUS001
        public string CompanyName { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Scale { get; set; }
        public string? Address { get; set; }
        public string ContactName { get; set; } = null!;
        public string? ContactTitle { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool VIP { get; set; }

        public int? AssignedToUserID { get; set; }
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("AssignedToUserID")]
        public User? AssignedUser { get; set; }
        public ICollection<Deal> Deals { get; set; } = new List<Deal>();
    }
}
