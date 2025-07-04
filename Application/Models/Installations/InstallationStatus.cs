namespace Application.Models.Installations;

public class InstallationStatus
{
    public string StageName { get; set; }
    public string Message { get; set; }
    public bool IsCompleted { get; set; }
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; }

    public InstallationStatus()
    {
        StageName = "Initializing";
        Message = "Starting installation process...";
        IsCompleted = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }
}
