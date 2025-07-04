namespace Application.Models.FileUploads
{
    public class UploadSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string ConnectionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public FileStream? FileStream { get; set; }
        public DateTime StartTime { get; set; }
        public long BytesReceived { get; set; }
        public UploadState State { get; set; } = UploadState.Initialized;
        public int TotalChunks { get; set; }
        public int ChunksReceived { get; set; } = 0;
        public HashSet<int> ReceivedChunkNumbers { get; set; } = new();
        public object LockObject => _lockObject;
        public string ExpectedChecksum { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public DateTime LastActivityTime { get; set; }
        public int RetryCount { get; set; } = 0;
        public string? LastError { get; set; }
        public bool _disposed = false;
        public bool IsComplete => ChunksReceived == TotalChunks && TotalChunks > 0;
        public double GetProgressPercentage() =>
            FileSize > 0 ? (BytesReceived * 100.0 / FileSize) : 0;
        private readonly object _lockObject = new object();
        public void UpdateProgress(long bytesWritten)
        {
            lock (_lockObject)
            {
                BytesReceived += bytesWritten;
                LastActivityTime = DateTime.UtcNow;
            }
        }
        public bool TryAddChunk(int chunkNumber)
        {
            lock (_lockObject)
            {
                if (ReceivedChunkNumbers.Contains(chunkNumber))
                    return false; // Duplicate chunk

                ReceivedChunkNumbers.Add(chunkNumber);
                ChunksReceived++;
                return true;
            }
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                FileStream?.Dispose();
                _disposed = true;
            }
        }
    }

    public enum UploadState
    {
        Initialized,
        Uploading,
        Finalizing,
        Completed,
        Failed,
        Cancelled
    }
}
