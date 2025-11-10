namespace Customer_Relationship_Management.ViewModels.User
{
    public class UserViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public bool TwoFAEnabled { get; set; }
    }
}
