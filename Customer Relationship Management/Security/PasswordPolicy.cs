using System.Text.RegularExpressions;

namespace Customer_Relationship_Management.Security
{
    public static class PasswordPolicy
    {
        public static bool IsStrongPassword(string password)
        {
            // Ít nhất 8 ký tự
            if (password.Length < 6) return false;

            // Có ít nhất 1 chữ hoa
            if (!Regex.IsMatch(password, "[A-Z]")) return false;

            // Có ít nhất 1 chữ thường
            if (!Regex.IsMatch(password, "[a-z]")) return false;

            // Có ít nhất 1 số
            if (!Regex.IsMatch(password, "[0-9]")) return false;

            // Có ít nhất 1 ký tự đặc biệt
            if (!Regex.IsMatch(password, @"[\W_]")) return false;

            return true;


            // sử dụng như sau:
       /*     if (!PasswordPolicy.IsStrongPassword(userPassword))
            {
                // Báo lỗi: mật khẩu không đủ mạnh
            }
            else
            {
                var hash = PasswordHasher.HashPassword(userPassword);
                // Lưu hash vào DB
            }
       */

        }
    }
}
