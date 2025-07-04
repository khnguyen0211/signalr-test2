using System.Collections.Concurrent;
using Application.Constants;
using Application.Helpers;
using Application.Models.FileUploads;
using Application.Models.FileValidations;
using Application.Services.Interfaces;

namespace Application.Services.Implementations
{
    public class FileUploadService : IFileUploadService
    {
        private static readonly Lazy<FileUploadService> _instance = new(() => new FileUploadService());
        public static FileUploadService Instance => _instance.Value;
        private readonly ConcurrentDictionary<string, UploadSession> _activeSessions = new();
        private readonly IFileValidationService _validationService;
        private readonly IEncryptionService _encryptionService;
        private readonly IExtractionService _extractionService;
        private readonly ILoggerService _logger;
        private FileUploadService()
        {
            _validationService = FileValidationService.Instance;
            _encryptionService = EncryptionService.Instance;
            _extractionService = ExtractionService.Instance;
            _logger = new LoggerService(Serilog.Log.Logger);
        }
        private string GetConnectionFolder(string connectionId)
        {
            return Path.Combine(Path.GetTempPath(), HashHelper.GetHashString(connectionId));
        }
        public UploadSession StartUploadAsync(string connectionId, UploadMetaData metaData)
        {
            // Check if there's already an active session for this connection
            if (_activeSessions.TryGetValue(connectionId, out var existingSession))
            {
                if (existingSession.State == UploadState.Uploading)
                    throw new InvalidOperationException(Messages.FileValidation.UploadInProgress);

                // Clean up old session before starting new one
                CleanupSessionInternal(existingSession);
                _activeSessions.TryRemove(connectionId, out _);
            }
            string uploadFolder = GetConnectionFolder(connectionId);
            // Validate input
            FileValidationResult validation = _validationService.ValidateUpload(metaData);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);
            // Ensure upload directory exists
            try
            {
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw new InvalidOperationException(Messages.FileValidation.FailedToCreateDirectory);
            }

            // Create safe file path
            var safeFileName = GenerateFileName.GenerateSafeFileName(metaData.FileName);
            var filePath = Path.Combine(uploadFolder, safeFileName);

            // Validate path to prevent directory traversal
            var fullPath = Path.GetFullPath(filePath);
            var uploadFolderPath = Path.GetFullPath(uploadFolder);
            if (!fullPath.StartsWith(uploadFolderPath))
                throw new InvalidOperationException(Messages.FileValidation.InvalidFilePath);

            FileStream? fileStream = null;
            try
            {
                // Create file stream with proper settings
                fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true);

                // Calculate total chunks
                var chunkSize = metaData.ChunkSize;
                var totalChunks = (int)Math.Ceiling((double)metaData.FileSize / chunkSize);

                // Create upload session
                var session = new UploadSession
                {
                    ConnectionId = connectionId,
                    FileName = metaData.FileName,
                    FilePath = filePath,
                    FileSize = metaData.FileSize,
                    FileStream = fileStream,
                    StartTime = DateTime.UtcNow,
                    LastActivityTime = DateTime.UtcNow,
                    BytesReceived = 0,
                    State = UploadState.Initialized,
                    TotalChunks = totalChunks,
                    ChunkSize = chunkSize,
                    ExpectedChecksum = metaData.ExpectedChecksum
                };

                // Add to active sessions
                if (!_activeSessions.TryAdd(connectionId, session))
                    throw new InvalidOperationException(Messages.Session.FailedToCreateSession);

                session.State = UploadState.Uploading;
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                // Clean up resources if session creation fails
                fileStream?.Dispose();
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
                throw;
            }
        }

        public async Task<ProcessChunkResult> ProcessChunkAsync(string connectionId, byte[] encryptedChunk, int chunkNumber)
        {
            if (!_activeSessions.TryGetValue(connectionId, out var session))
                throw new InvalidOperationException(Messages.Session.InactiveSession);

            // Validate session state
            if (session.State != UploadState.Uploading)
                throw new InvalidOperationException(Messages.Session.InvalidSessionState);

            if (session.FileStream == null)
                throw new InvalidOperationException(Messages.FileValidation.InvalidFileStream);

            // Use lock for thread safety
            lock (session.LockObject)
            {
                // Validate chunk number
                if (chunkNumber < 0 || chunkNumber >= session.TotalChunks)
                    throw new ArgumentOutOfRangeException(nameof(chunkNumber),
                        Messages.FileValidation.InvalidChunkNumber);

                // Check for duplicate chunk
                if (!session.TryAddChunk(chunkNumber))
                {
                    return new ProcessChunkResult
                    {
                        Success = false,
                        IsDuplicate = true,
                        Progress = session.GetProgressPercentage()
                    };
                }
            }
            try
            {
                // Decrypt chunk
                byte[] decryptedData;
                try
                {
                    decryptedData = _encryptionService.DecryptChunk(connectionId, encryptedChunk);
                }
                catch (Exception ex)
                {
                    session.LastError = $"{Messages.Encryption.FailedToDecrypt}: {ex.Message}";
                    throw new InvalidOperationException(Messages.Encryption.FailedToDecrypt, ex);
                }

                // Calculate position in file
                long position = (long)chunkNumber * session.ChunkSize;

                // Write to file at correct position
                await WriteChunkToFileAsync(session, decryptedData, position);

                // Update progress
                session.UpdateProgress(decryptedData.Length);
                var result = new ProcessChunkResult
                {
                    Success = true,
                    ChunksReceived = session.ChunksReceived,
                    TotalChunks = session.TotalChunks,
                    Progress = session.GetProgressPercentage(),
                };

                return result;
            }
            catch (Exception ex)
            {
                session.RetryCount++;
                session.LastError = ex.Message;

                if (session.RetryCount >= SocketConfigs.FileUploadConfigs.MaxRetryCount)
                {
                    session.State = UploadState.Failed;
                    await CleanupConnectionAsync(connectionId);
                }

                throw;
            }
        }

        private async Task WriteChunkToFileAsync(UploadSession session, byte[] data, long position)
        {
            if (session.FileStream == null)
                throw new InvalidOperationException(Messages.FileValidation.InvalidFileStream);

            // Seek to correct position
            session.FileStream.Seek(position, SeekOrigin.Begin);

            // Write data
            await session.FileStream.WriteAsync(data, 0, data.Length);

            // Force flush for important data
            if (session.ChunksReceived % 10 == 0)
                await session.FileStream.FlushAsync();
        }

        public async Task<string> EndUploadAsync(string connectionId)
        {
            if (!_activeSessions.TryGetValue(connectionId, out var session))
                throw new InvalidOperationException(Messages.Session.InactiveSession);

            lock (session.LockObject)
            {
                // Validate session state
                if (session.State != UploadState.Uploading)
                {
                    throw new InvalidOperationException(MessageFormatter.Format(Messages.FileValidation.CannotUploadInState, session.State));
                }

                // Check if all chunks received
                if (!session.IsComplete)
                {
                    var missingChunks = Enumerable.Range(0, session.TotalChunks)
                        .Except(session.ReceivedChunkNumbers)
                        .ToList();

                    throw new InvalidOperationException(
                        $"{Messages.FileValidation.IncompleteUpload} {string.Join(", ", missingChunks)}");
                }

                session.State = UploadState.Finalizing;
            }

            try
            {
                // Flush and close file stream
                if (session.FileStream != null)
                {
                    await session.FileStream.FlushAsync();
                    await session.FileStream.DisposeAsync();
                    session.FileStream = null;
                }

                // Validate file size
                var fileInfo = new FileInfo(session.FilePath);
                if (!fileInfo.Exists || fileInfo.Length != session.FileSize)
                    throw new InvalidOperationException(
                        MessageFormatter.Format(Messages.FileValidation.FileSizeMissMatch, session.FileSize, fileInfo.Length)
                        );

                // Validate checksum
                bool isValid = _validationService.ValidateChecksum(session.FilePath, session.ExpectedChecksum);

                if (!isValid)
                {
                    session.State = UploadState.Failed;
                    throw new InvalidOperationException(Messages.FileValidation.InvalidChecksum);
                }

                session.State = UploadState.Completed;

                await _extractionService.ExtractArchiveAsync(session.FilePath, connectionId);
                // Remove from active sessions but keep the file
                _activeSessions.TryRemove(connectionId, out _);

                return $"{Messages.FileValidation.UploadSuccessful} {session.FilePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Messages.FileValidation.FailedToUpload} {session.FileName}");

                // Clean up on failure
                await CleanupConnectionAsync(connectionId);
                throw;
            }
        }

        private void CleanupSessionInternal(UploadSession session)
        {
            try
            {
                // Dispose file stream
                session.FileStream?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.Session.DisposingError);
            }

            // Delete incomplete file
            if (session.State != UploadState.Completed && File.Exists(session.FilePath))
            {
                try
                {
                    File.Delete(session.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{Messages.FileValidation.FailedToDelete} {session.FilePath}");
                }
            }

            session.Dispose();
        }

        public async Task CleanupConnectionAsync(string connectionId)
        {
            if (_activeSessions.TryRemove(connectionId, out var session))
            {
                await Task.Run(() => CleanupSessionInternal(session));
                // Remove encryption key
                _encryptionService.RemoveEncryptionKey(connectionId);
            }
        }

    }

}