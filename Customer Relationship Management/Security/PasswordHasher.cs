namespace Customer_Relationship_Management.Security
{
    public static class PasswordHasher
    {
        // Hash password
        public static string HashPassword(string password)
        {
            // Bạn có thể điều chỉnh WorkFactor để tăng/giảm độ mạnh
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        // Verify password khi login
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
