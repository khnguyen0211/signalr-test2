using Application.Constants;
using Application.Helpers;
using Application.Models.FileUploads;
using Application.Models.FileValidations;
using Application.Services.Interfaces;

namespace Application.Services.Implementations
{

    public class FileValidationService : IFileValidationService
    {
        private static readonly Lazy<FileValidationService> _instance = new(() => new FileValidationService());
        public static FileValidationService Instance => _instance.Value;

        // Updated to include more archive formats
        private readonly string[] AllowedExtensions = SocketConfigs.FileUploadConfigs.AllowedFileExtensions;
        private readonly long MaxFileSize = SocketConfigs.FileUploadConfigs.MaxFileSize;
        private readonly long MaxChunkSize = SocketConfigs.FileUploadConfigs.MaxChunkSize;
        private readonly HashSet<char> InvalidChars = SocketConfigs.FileUploadConfigs.InvalidChars;

        public bool ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (fileName.Length > 255)
                return false;

            if (fileName.Any(c => InvalidChars.Contains(c)))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        public bool ValidateFileSize(long fileSize)
        {
            return fileSize > 0 && fileSize <= MaxFileSize;
        }


        public bool ValidateChunkSize(long chunkSize)
        {
            return chunkSize > 0 && chunkSize <= MaxChunkSize;
        }

        public bool ValidateChecksum(string filePath, string expectedChecksum)
        {
            if (!File.Exists(filePath) || string.IsNullOrWhiteSpace(expectedChecksum))
                return false;

            var actualChecksum = ChecksumCalculator.CalculateFileChecksum(filePath);
            return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        public FileValidationResult ValidateUpload(UploadMetaData metaData)
        {
            if (!ValidateFileName(metaData.FileName))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = Messages.FileValidation.InvalidFileName
                };
            }

            if (!ValidateFileSize(metaData.FileSize))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = Messages.FileValidation.InvalidFileSize
                };
            }
            if (!ValidateFileSize(metaData.ChunkSize))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = Messages.FileValidation.InvalidChunkSize
                };
            }

            return new FileValidationResult
            {
                IsValid = true,
            };
        }


    }

}
