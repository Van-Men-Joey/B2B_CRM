public class AdminUserControlViewModel
{
    public int UserID { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public bool ForceChangePassword { get; set; }
    public bool TwoFAEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
}
