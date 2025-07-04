using System.Text;
using Application.Constants;

namespace Application.Helpers
{
    public static class GenerateFileName
    {
        public static string GenerateSafeFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            // Remove invalid characters
            var safeNameBuilder = new StringBuilder();
            foreach (var c in nameWithoutExtension)
            {
                if (!SocketConfigs.FileUploadConfigs.InvalidChars.Contains(c))
                    safeNameBuilder.Append(c);
            }
            var safeName = safeNameBuilder.ToString();
            string hashedName = HashHelper.GetHashString(safeName);
            return $"{hashedName}{extension}";
        }
    }
}
