using System.Security.Cryptography;

namespace Application.Helpers
{
    public static class ChecksumCalculator
    {
        public static string CalculateFileChecksum(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
