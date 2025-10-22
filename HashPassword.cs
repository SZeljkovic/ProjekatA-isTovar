using System.Security.Cryptography;
using System.Text;

namespace Projekat_B_isTovar
{
    public static class HashPassword
    {
        public static string HashSHA256(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2")); // x2 formatira bajt kao dva heksadecimalna znaka
                }
                return builder.ToString();
            }
        }
    }
}
