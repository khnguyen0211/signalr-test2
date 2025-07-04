using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Application.Constants;

namespace Application.Helpers
{
    public class CertificateCreator
    {
        public static async Task<bool> CreateAndExportCertificateAsync(string certificatePath, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(certificatePath))
                    throw new ArgumentException(Messages.Certificate.InvalidFilePath);

                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException(Messages.Certificate.InvalidPassword);

                var directory = Path.GetDirectoryName(certificatePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = SocketConfigs.ServerConfigs.ProcessName,
                    Arguments = MessageFormatter.Format(SocketConfigs.ServerConfigs.ProcessCommand, certificatePath, password),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool IsCertificateValid(string certificatePath, string password)
        {
            try
            {
                if (!File.Exists(certificatePath))
                    return false;

                X509Certificate2 certificate;

                if (!string.IsNullOrEmpty(password))
                    certificate = new X509Certificate2(certificatePath, password);
                else
                    certificate = new X509Certificate2(certificatePath);

                var now = DateTime.Now;

                return now >= certificate.NotBefore && now <= certificate.NotAfter;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }

}
