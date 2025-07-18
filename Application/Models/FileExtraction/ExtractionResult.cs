﻿namespace Application.Models.FileExtraction
{
    public class ExtractionResult
    {
        public bool IsSuccess { get; set; }
        public string ExtractionPath { get; set; } = string.Empty;
        public List<ExtractedFile> ExtractedFiles { get; set; } = new();
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ExtractionTime { get; set; }
    }
}
