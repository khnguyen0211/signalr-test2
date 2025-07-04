namespace Application.Models.Installations
{
    public enum ItemStatus
    {
        Pending,
        Installing,
        Completed,
        Failed
    }

    public class InstallationItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ScriptFolderPath { get; set; } = string.Empty;
        public ItemStatus Status { get; set; } = ItemStatus.Pending;
        public DateTime? StartedTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string DisplayName => $"{ItemId} - {Version}";
    }
}
