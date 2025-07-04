namespace Application.Services.Interfaces
{
    public interface IConnectionManagerService
    {
        bool TryRegisterConnection(string connectionId);
        bool UnregisterConnection(string connectionId);
        bool IsConnectionActive(string connectionId);
        IEnumerable<string> GetActiveConnections();
        bool HasActiveConnection();
        string? GetCurrentActiveConnection();
        int GetMaxAllowedConnections();
    }
}
