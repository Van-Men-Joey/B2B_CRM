namespace Customer_Relationship_Management.ViewModels.User
{
    public class UpdateUserViewModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Status { get; set; } = "Active";
        public bool TwoFAEnabled { get; set; }
    }
}
