using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // 👈 Quan trọng: để dùng [NotMapped]
using Customer_Relationship_Management.Repositories.Interfaces; // Namespace chứa IEntity

namespace Customer_Relationship_Management.Models
{
    public class Role : IEntity
    {
        [Key]
        public int RoleID { get; set; }

        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();

        // 👇 BỔ SUNG ĐOẠN NÀY ĐỂ FIX LỖI 👇

        // Định nghĩa thuộc tính Id theo yêu cầu của IEntity
        // [NotMapped] nghĩa là: "Chỉ dùng trong code C#, đừng tìm cột này trong Database"
        [NotMapped]
        public int Id
        {
            get { return RoleID; }
            set { RoleID = value; }
        }
    }
}