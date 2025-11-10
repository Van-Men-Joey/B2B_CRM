using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.ViewModels.User
{
    public class UserProfileViewModel
    {
        public int UserID { get; set; }

        [Display(Name = "Mã người dùng")]
        public string? UserCode { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? Phone { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarPath { get; set; }

        [Display(Name = "Vai trò")]
        public string? RoleName { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Mật khẩu cũ")]
        [DataType(DataType.Password)]
        public string? OldPassword { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
