namespace Application.Models.FileUploads
{
    public class ProcessChunkResult
    {
        public bool Success { get; set; }
        public bool IsDuplicate { get; set; }
        public int ChunksReceived { get; set; }
        public int TotalChunks { get; set; }
        public double Progress { get; set; }
        public double UploadSpeedBps { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }
}
