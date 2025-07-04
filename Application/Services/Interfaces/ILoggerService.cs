namespace Application.Services.Interfaces;

public interface ILoggerService
{
    public void LogDebug(string message);
    public void LogInformation(string message);
    public void LogError(Exception exception);
    public void LogError(string errorMessage);
    public void LogError(Exception exception, string errorMessage);
    public void LogVerbose(string message);
}
