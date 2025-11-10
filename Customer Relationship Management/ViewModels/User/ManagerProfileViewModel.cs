namespace Customer_Relationship_Management.ViewModels.User
{
    public class ManagerProfileViewModel
    {
        public int UserID { get; set; }
        public string? UserCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
