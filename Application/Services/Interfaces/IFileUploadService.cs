using Application.Models.FileUploads;

namespace Application.Services.Interfaces
{
    public interface IFileUploadService
    {
        UploadSession StartUploadAsync(string connectionId, UploadMetaData metaData);
        Task<ProcessChunkResult> ProcessChunkAsync(string connectionId, byte[] encryptedChunk, int chunkNumber);
        Task<string> EndUploadAsync(string connectionId);
    }
}
