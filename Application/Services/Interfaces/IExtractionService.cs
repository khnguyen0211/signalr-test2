using Application.Models.FileExtraction;

namespace Application.Services.Interfaces
{
    public interface IExtractionService
    {
        Task<ExtractionResult> ExtractArchiveAsync(string archivePath, string connectionId);
    }
}
