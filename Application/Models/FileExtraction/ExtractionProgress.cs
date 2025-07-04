namespace Application.Models.FileExtraction
{
    public class ExtractionProgress
    {
        public string CurrentFile { get; set; } = string.Empty;
        public int ProcessedEntries { get; set; }
        public int TotalEntries { get; set; }
        public long ProcessedBytes { get; set; }
        public long TotalBytes { get; set; }
    }
}