using Application.Constants;
using Application.Helpers;
using Application.Models.SystemInformation;
using Application.Services.Interfaces;
using System.Text.Json;

namespace Application.Services.Implementations
{
    public class SystemInformationService : ISystemInformationService
    {
        private static readonly Lazy<SystemInformationService> _instance = new(() => new SystemInformationService());
        public static SystemInformationService Instance => _instance.Value;
        private readonly ILoggerService _logger;
        private SystemInformationService()
        {
            _logger = new LoggerService(Serilog.Log.Logger);
        }
        public SystemInfo GetWindowsSystemInfo()
        {
            try
            {
                var output = PowerShellHelper.ExecutePowerShellScript(PowerShellScript.GetSystemInformation);
                var systemInfo = JsonSerializer.Deserialize<SystemInfo>(output, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return systemInfo ?? new SystemInfo();
            }
            catch (Exception ex)
            {
                throw new Exception($"{Messages.ShellExecution.SystemInfoRetrievalFailed} {ex.Message}", ex);
            }
        }
    }
}
