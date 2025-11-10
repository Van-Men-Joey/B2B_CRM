namespace Customer_Relationship_Management.ViewModels.User
{
    public class ManagerAccountControlViewModel
    {
        public int UserID { get; set; }
        public string UserCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public bool ForceChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
