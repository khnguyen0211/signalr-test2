using System.Security.Cryptography;
using System.Text;

namespace Application.Helpers
{
    public class HashHelper
    {
        public static string GetHashString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
