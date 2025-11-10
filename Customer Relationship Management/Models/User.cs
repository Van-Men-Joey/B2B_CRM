using Customer_Relationship_Management.Repositories.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace Customer_Relationship_Management.Models
{
    public class User : IEntity
    {
        [Key]
        public int UserID { get; set; }
        // ⚙️ Thêm dòng này để khớp với GenericRepository
        public int Id => UserID;
        public string? UserCode { get; set; }  // EMP001 / MAN001 / ADM001
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }

        public int RoleID { get; set; }
        public string Status { get; set; } = "Active";
        public bool ForceChangePassword { get; set; }
        public bool TwoFAEnabled { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? ManagerID { get; set; }
        public User? Manager { get; set; }  // Navigation property
        public ICollection<User>? Employees { get; set; } // Nếu muốn xem danh sách nhân viên dưới quyền
        // Navigation
        public Role Role { get; set; } = null!;
        public ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
        public ICollection<Deal> CreatedDeals { get; set; } = new List<Deal>();
        public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
        public ICollection<Contract> ContractsCreated { get; set; } = new List<Contract>();
        public ICollection<Contract> ContractsApproved { get; set; } = new List<Contract>();
    }
}
