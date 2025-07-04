namespace Application.Constants
{
    public static class SocketConfigs
    {
        public static class SocketActions
        {
            public const string BootstrapToWeb = "BootstrapToWeb";
            public const string ReceiveInstallationStatus = "ReceiveInstallationStatus";
            public const string InitializedApplicationStatus = "InitializedApplicationStatus";
            public const string InstalledApplicationVersion = "InstalledApplicationVersion";
            public const string ReadSystemInfo = "ReadSystemInfo";
            public const string SetEncryptionKey = "SetEncryptionKey";
            public const string ReportSessionStatus = "ReportSessionStatus";
        }

        public static class ServerConfigs
        {
            public const string ClientHost = "http://localhost:3000";
            public const int ServerPort = 5701;
            public const int MaxConnection = 1;
            public const string CertFileName = "cert.pfx";
            public static string CertPath => Path.Combine(Path.GetTempPath(), CertFileName);
            public const string Password = "010011010";
            public const string ProcessName = "dotnet";
            public static string ProcessCommand = "dev-certs https --export-path {0} --password {1}";
        }

        public static class FileUploadConfigs
        {
            public static readonly string[] AllowedFileExtensions = {
                ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
                ".tar.gz", ".tar.bz2", ".tgz", ".bundle"
            };

            public static readonly HashSet<char> InvalidChars = new HashSet<char> {
                '<', '>', ':', '"', '|', '?', '*', '/', '\\'
            };

            public const int MaxFileSize = 10 * 1024 * 1024; //10 MB
            public const int MaxChunkSize = 100 * 1024; //100 KB
            public const int SessionTimeOutMinute = 30;
            public const int MaxRetryCount = 3;
        }

        public static class StageFiles
        {
            public const string PreInstallation = "pre-config";
            public const string Installation = "install";
            public const string PostInstallation = "post-install";
        }

        public static class WindowPaths
        {
            public static readonly string[] RegistryPaths = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };
        }
    }
}
