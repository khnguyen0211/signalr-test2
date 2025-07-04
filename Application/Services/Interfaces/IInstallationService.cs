using Application.Models.Installations;

namespace Application.Services.Interfaces
{
    public interface IInstallationService
    {
        Task<object> SequentialInstallation(string ConnectionId, List<InstallApplication> installApplicationList);
        Task<object> ControlInstall(string action);
        Task<SessionReportStatus> GetSessionStatus(InstallationSession session);
        Task<string> GetOrCreateGlobalSessionAsync();
        Task SetBaseFolderPathAsync(string baseFolderPath);
        Task AssociateConnectionAsync(string connectionId);
        Task DisconnectAsync(string connectionId);
        Task SetupConnectionId(string connectionId);
        Task InitializeSessionFromApplicationsAsync(List<InstallApplication> installApplicationList);
        Task StartOrResumeProcessingAsync();
        Task CompleteCurrentItemAndProcessNextAsync();
        Task FailCurrentItemAsync(string errorMessage);
        Task<List<InstallationItem>> GetAllItemsAsync();
        Task<InstallationSession?> GetGlobalSessionAsync();
        Task<bool> HasActiveSessionAsync();
        Task ClearGlobalSessionAsync();
        Task StopProcessingAsync();
        Task ContinueProcessingAsync();

    }
}