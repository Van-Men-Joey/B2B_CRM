namespace Customer_Relationship_Management.ViewModels.User
{
    public class EmployeeProfileViewModel
    {
        public int UserID { get; set; }
        public string? UserCode { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }

        public string RoleName { get; set; } = null!;
        public bool TwoFAEnabled { get; set; }

        // Đổi mật khẩu
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }

        // Notification setting (mở rộng sau)
        public bool NotifyDealDeadline { get; set; }
    }
}
