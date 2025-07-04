using System.Collections.Concurrent;
using Application.Constants;
using Application.Helpers;
using Application.Models.FileExtraction;
using Application.Services.Interfaces;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Application.Services.Implementations
{
    public class ExtractionService : IExtractionService
    {
        private static readonly Lazy<ExtractionService> _instance = new(() => new ExtractionService());
        public static ExtractionService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, ExtractionProgress> _extractionProgress = new();
        private readonly ILoggerService _logger;

        public ExtractionService()
        {
            _logger = new LoggerService(Serilog.Log.Logger);
        }

        public async Task<ExtractionResult> ExtractArchiveAsync(string archivePath, string connectionId)
        {
            var result = new ExtractionResult { ExtractionTime = DateTime.UtcNow };
            try
            {
                if (!File.Exists(archivePath))
                    throw new FileNotFoundException($"{Messages.FileValidation.FileNotFound} {archivePath}");

                var fileName = Path.GetFileNameWithoutExtension(archivePath);
                var extractPath = GetExtractionPath(connectionId, fileName);
                EnsureDirectoryExists(extractPath);
                result.ExtractionPath = extractPath;

                // Initialize progress tracking
                var progress = new ExtractionProgress();
                _extractionProgress[connectionId] = progress;
                await ExtractGeneralArchiveAsync(archivePath, extractPath, progress);

                // Collect extraction results
                result.ExtractedFiles = await GetExtractedFilesAsync(extractPath);
                result.FileCount = result.ExtractedFiles.Count(f => !f.IsDirectory);
                result.TotalSize = result.ExtractedFiles.Sum(f => f.Size);
                result.IsSuccess = true;

                // Remove progress tracking
                _extractionProgress.TryRemove(connectionId, out _);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _extractionProgress.TryRemove(connectionId, out _);
            }

            return result;
        }

        private async Task ExtractGeneralArchiveAsync(string archivePath, string extractPath, ExtractionProgress progress)
        {
            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.Open(archivePath);
                progress.TotalEntries = archive.Entries.Count();
                progress.TotalBytes = archive.TotalUncompressSize;
                var options = new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true,
                    PreserveFileTime = true
                };
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        if (entry != null && entry.Key != null)
                        {
                            progress.CurrentFile = entry.Key;
                            entry.WriteToDirectory(extractPath, options);
                            progress.ProcessedEntries++;
                            progress.ProcessedBytes += entry.Size;
                        }

                    }
                }
            });
        }

        public async Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath)
        {
            var info = new ArchiveInfo();
            try
            {
                await Task.Run(() =>
                {
                    var fileInfo = new FileInfo(archivePath);
                    info.CompressedSize = fileInfo.Length;
                    using var archive = ArchiveFactory.Open(archivePath);
                    info.ArchiveType = archive.Type.ToString();
                    info.UncompressedSize = archive.TotalUncompressSize;
                    info.EntryCount = archive.Entries.Count();
                    info.IsPasswordProtected = archive.Entries.Any(e => e.IsEncrypted);
                    foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                    {
                        if (entry != null && entry.Key != null)
                            info.FileList.Add(entry.Key);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }

            return info;
        }

        public string GetExtractionPath(string connectionId, string fileName)
        {
            string extractFolder = GetConnectionFolder(connectionId);
            var timestamp = DateTime.UtcNow.Ticks;
            var safeName = Path.GetFileNameWithoutExtension(fileName);
            return Path.Combine(extractFolder, safeName);
        }

        private async Task<List<ExtractedFile>> GetExtractedFilesAsync(string extractPath)
        {
            var files = new List<ExtractedFile>();

            await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(extractPath);
                var allItems = directoryInfo.GetFileSystemInfos("*", SearchOption.AllDirectories);

                foreach (var item in allItems)
                {
                    var relativePath = Path.GetRelativePath(extractPath, item.FullName);
                    files.Add(new ExtractedFile
                    {
                        FileName = item.Name,
                        RelativePath = relativePath,
                        FullPath = item.FullName,
                        Size = item is FileInfo fileInfo ? fileInfo.Length : 0,
                        LastModified = item.LastWriteTimeUtc,
                        IsDirectory = item is DirectoryInfo
                    });
                }
            });

            return files;
        }
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        private string GetConnectionFolder(string connectionId)
        {
            return Path.Combine(Path.GetTempPath(), HashHelper.GetHashString(connectionId));
        }
    }
}
