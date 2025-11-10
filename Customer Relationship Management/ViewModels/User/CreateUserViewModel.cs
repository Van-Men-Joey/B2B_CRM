namespace Customer_Relationship_Management.ViewModels.User
{
    public class CreateUserViewModel
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Role { get; set; } = "Employee";
        public string Password { get; set; } = null!;
    }
}
