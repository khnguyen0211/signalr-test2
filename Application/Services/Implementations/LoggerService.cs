using Application.Services.Interfaces;

namespace Application.Services.Implementations;

public class LoggerService : ILoggerService
{
    private readonly Serilog.ILogger _logger;
    public LoggerService(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    void ILoggerService.LogDebug(string message)
    {
        _logger.Debug(message);
    }

    void ILoggerService.LogError(Exception exception)
    {
        _logger.Error(exception, exception.Message);
    }

    void ILoggerService.LogError(string errorMessage)
    {
        _logger.Error(errorMessage);
    }

    void ILoggerService.LogError(Exception exception, string errorMessage)
    {
        _logger.Error(exception, errorMessage);
    }

    void ILoggerService.LogInformation(string message)
    {
        _logger.Information(message);
    }

    void ILoggerService.LogVerbose(string message)
    {
        _logger.Verbose(message);
    }
}
