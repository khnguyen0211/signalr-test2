namespace Application.Models.FileUploads
{
    public class UploadMetaData
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int ChunkSize { get; set; }
        public string ExpectedChecksum { get; set; } = string.Empty;
    }
}
