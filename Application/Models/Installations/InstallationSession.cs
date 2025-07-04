namespace Application.Models.Installations
{
    public enum SessionStatus
    {
        Created,
        Running,
        Paused,
        Completed,
        Failed
    }

    public class InstallationSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string BaseFolderPath { get; set; } = string.Empty;
        public string? CurrentConnectionId { get; set; }
        public bool IsClientConnected { get; set; } = false;
        public bool IsManuallyPaused { get; set; } = false;
        public List<InstallationItem> InstallationItemList { get; set; } = new();
        public InstallationItem? CurrentProcessingItem { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Created;
        public bool CanProcessNext() =>
            IsClientConnected && !IsManuallyPaused &&
            (CurrentProcessingItem == null ||
             CurrentProcessingItem.Status == ItemStatus.Completed ||
             CurrentProcessingItem.Status == ItemStatus.Failed);

        public bool IsAllItemsCompleted() =>
            InstallationItemList.Count > 0 &&
            InstallationItemList.All(x => x.Status == ItemStatus.Completed ||
                                  x.Status == ItemStatus.Failed);
    }


}
