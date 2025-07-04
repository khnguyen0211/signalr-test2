namespace Application.Models.Installations
{
    public class SessionReportStatus
    {
        public string SessionId { get; set; } = String.Empty;
        public string SessionStatus { get; set; } = String.Empty;
        public List<ProgressStatus> ItemList { get; set; } = new List<ProgressStatus>();
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public int FailedItems { get; set; }
        public int PendingItems { get; set; }
    }

    public class ProgressStatus
    {
        public string Id { get; set; } = String.Empty;
        public string Version { get; set; } = String.Empty;
        public int? InstallProgress { get; set; }
        public string Status { get; set; } = String.Empty;
    }
}
