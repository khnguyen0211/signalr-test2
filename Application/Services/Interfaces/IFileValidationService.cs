using Application.Models.FileUploads;
using Application.Models.FileValidations;

namespace Application.Services.Interfaces
{
    public interface IFileValidationService
    {
        bool ValidateFileName(string fileName);
        bool ValidateFileSize(long fileSize);
        bool ValidateChecksum(string filePath, string expectedChecksum);
        FileValidationResult ValidateUpload(UploadMetaData metaData);
    }
}
