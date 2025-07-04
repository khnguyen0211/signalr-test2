using System.Runtime.InteropServices;
using Application.Helpers;
using Application.Hubs;
using Application.Models.Installations;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Application.Services.Implementations
{
    public class InstallationService : IInstallationService
    {
        private static InstallationSession? _globalSession = null;
        private readonly object _lockObject = new object();
        private readonly ILoggerService _logger;
        private readonly IBackgroundTaskService _backgroundService;
        private readonly IHubContext<BootstrapHub> _hubContext;
        private readonly IBootstrapService _bootstrapService;
        public InstallationService(ILoggerService logger,
            IBackgroundTaskService backgroundService,
            IHubContext<BootstrapHub> hubContext,
            IBootstrapService bootstrapService)
        {
            _logger = logger;
            _hubContext = hubContext;
            _backgroundService = backgroundService;
            _bootstrapService = bootstrapService;
        }

        public async Task<object> SequentialInstallation(string ConnectionId, List<InstallApplication> installApplicationList)
        {
            string[] versions = installApplicationList.Select(app => app.Version).ToArray();

            _logger.LogInformation($"[SequentialInstallation] Starting install for connection: {ConnectionId}");

            string sessionId = await GetOrCreateGlobalSessionAsync();
            _logger.LogInformation($"[SequentialInstallation] Using session: {sessionId}");

            string baseFolderPath = Path.Combine(Path.GetTempPath(), HashHelper.GetHashString(ConnectionId));
            await SetBaseFolderPathAsync(baseFolderPath);
            _logger.LogInformation($"[SequentialInstallation] Base path: {baseFolderPath}");

            await AssociateConnectionAsync(ConnectionId);
            _logger.LogInformation($"[SequentialInstallation] Associated connection with session");

            await InitializeSessionFromApplicationsAsync(installApplicationList);
            _logger.LogInformation($"[SequentialInstallation] Queue initialized with versions: {string.Join(", ", versions)}");

            await StartOrResumeProcessingAsync();
            _logger.LogInformation($"[SequentialInstallation] Started/resumed processing");

            var session = await GetGlobalSessionAsync();
            var itemList = await GetAllItemsAsync();

            var response = new
            {
                SessionId = sessionId,
                BasePath = baseFolderPath,
                SessionStatus = session?.Status.ToString(),
                IsClientConnected = session?.IsClientConnected,
                TestVersions = versions,
                ItemList = itemList.Select(x => new
                {
                    x.DisplayName,
                    x.Status,
                    x.StartedTime,
                    x.CompletedTime,
                    x.ErrorMessage
                }).ToList()
            };
            return response;
        }

        public Task<string> GetOrCreateGlobalSessionAsync()
        {
            lock (_lockObject)
            {
                if (_globalSession != null && _globalSession.Status != SessionStatus.Completed)
                {
                    _logger.LogInformation($"Reusing existing global session: {_globalSession.SessionId}");
                    return Task.FromResult(_globalSession.SessionId);
                }
                _logger.LogInformation("Create new session");
                _globalSession = new InstallationSession
                {
                    Status = SessionStatus.Running,
                    IsClientConnected = false
                };

                _logger.LogInformation($"Created new global session: {_globalSession.SessionId}");
                return Task.FromResult(_globalSession.SessionId);
            }
        }

        public Task SetBaseFolderPathAsync(string baseFolderPath)
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists. Call GetOrCreateGlobalSessionAsync first.");

                if (!Directory.Exists(baseFolderPath))
                    throw new DirectoryNotFoundException($"Base folder not found: {baseFolderPath}");

                if (string.IsNullOrEmpty(_globalSession.BaseFolderPath))
                {
                    _globalSession.BaseFolderPath = baseFolderPath;
                    _logger.LogInformation($"Set base path for global session: {baseFolderPath}");
                }
                else
                {
                    _logger.LogInformation($"Base path already set: {_globalSession.BaseFolderPath}");
                }
                return Task.CompletedTask;
            }
        }

        public Task AssociateConnectionAsync(string connectionId)
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                _globalSession.CurrentConnectionId = connectionId;
                _globalSession.IsClientConnected = true;

                if (_globalSession.Status == SessionStatus.Paused)
                {
                    _globalSession.Status = SessionStatus.Running;
                    _logger.LogInformation("Resumed global session");
                }

                _logger.LogInformation($"Associated connection {connectionId} with global session");
                return Task.CompletedTask;
            }
        }

        public Task DisconnectAsync(string connectionId)
        {
            lock (_lockObject)
            {
                if (_globalSession == null || _globalSession.CurrentConnectionId != connectionId)
                    return Task.CompletedTask;

                _globalSession.IsClientConnected = false;
                _globalSession.CurrentConnectionId = null;

                _logger.LogInformation($"Disconnected from global session: {connectionId}");

                return Task.CompletedTask;
            }
        }

        public Task SetupConnectionId(string connectionId)
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    return Task.CompletedTask;

                _globalSession.IsClientConnected = true;
                _globalSession.CurrentConnectionId = connectionId;

                _logger.LogInformation($"SetupConnectionId from global session: {connectionId}");

                return Task.CompletedTask;
            }
        }

        public Task InitializeSessionFromApplicationsAsync(List<InstallApplication> selectedApplications)
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                if (string.IsNullOrEmpty(_globalSession.BaseFolderPath))
                    throw new InvalidOperationException("Base folder path not set for global session");

                if (_globalSession.InstallationItemList.Count > 0)
                {
                    _logger.LogInformation("Queue already initialized. Skipping initialization.");
                    return Task.CompletedTask;
                }

                string platform = GetCurrentPlatform();
                _logger.LogInformation($"Detected platform: {platform}");

                var extractedFolders = Directory.GetDirectories(_globalSession.BaseFolderPath)
                                                .Where(dir => !dir.EndsWith(".bundle"))
                                                .Select(dir => new DirectoryInfo(dir))
                                                .ToList();

                _logger.LogInformation($"Found {extractedFolders.Count} extracted folders");

                foreach (var app in selectedApplications)
                {
                    _logger.LogInformation($"Processing app '{app.Id}' version '{app.Version}'");

                    var expectedFolderName = HashHelper.GetHashString(app.Id);
                    var targetFolder = extractedFolders.FirstOrDefault(f =>
                        f.Name.Equals(expectedFolderName, StringComparison.OrdinalIgnoreCase));

                    if (targetFolder == null)
                    {
                        _logger.LogInformation($"Folder not found for app '{app.Id}' (expected: {expectedFolderName})");
                        continue;
                    }

                    var folderName = targetFolder.Name;
                    _logger.LogInformation($"Found folder '{folderName}' for app '{app.Id}' (created: {targetFolder.CreationTime})");

                    var versionPath = Path.Combine(targetFolder.FullName, app.Version);
                    var scriptPath = Path.Combine(versionPath, platform);

                    _logger.LogInformation($"Checking script path: {scriptPath}");

                    if (Directory.Exists(scriptPath))
                    {
                        var scriptFiles = Directory.GetFiles(scriptPath, "*.ps1");
                        if (scriptFiles.Length > 0)
                        {
                            var queueItem = new InstallationItem
                            {
                                ItemId = app.Id,
                                ItemName = folderName,
                                Version = app.Version,
                                ScriptFolderPath = scriptPath,
                                Status = ItemStatus.Pending
                            };
                            _globalSession.InstallationItemList.Add(queueItem);
                            _logger.LogInformation($"Added to queue: {queueItem.DisplayName} -> {scriptPath}");
                        }
                        else
                        {
                            _logger.LogInformation($"No PowerShell scripts found in: {scriptPath}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Script path not found: {scriptPath}");
                    }
                }

                _logger.LogInformation($"Queue initialized with {_globalSession.InstallationItemList.Count} items from {selectedApplications.Count} applications");
                return Task.CompletedTask;
            }
        }


        public async Task StartOrResumeProcessingAsync()
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                if (_globalSession.Status == SessionStatus.Completed)
                {
                    _logger.LogInformation("Session already completed. Nothing to process.");
                    return;
                }

                _globalSession.Status = SessionStatus.Running;
                _logger.LogInformation("Started/resumed processing for global session");
            }

            await ProcessNextItemIfPossibleAsync();
        }

        public async Task CompleteCurrentItemAndProcessNextAsync()
        {
            InstallationItem? completedItem = null;

            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                if (_globalSession.CurrentProcessingItem != null)
                {
                    _globalSession.CurrentProcessingItem.Status = ItemStatus.Completed;
                    completedItem = _globalSession.CurrentProcessingItem;
                    _globalSession.CurrentProcessingItem.CompletedTime = DateTime.UtcNow;
                    _logger.LogInformation($"Completed item: {_globalSession.CurrentProcessingItem.DisplayName}");
                    _globalSession.CurrentProcessingItem = null;
                }
            }
            if (completedItem != null)
                await NotifyInstallProgress(completedItem, 100);

            await ProcessNextItemIfPossibleAsync();
        }

        public async Task FailCurrentItemAsync(string errorMessage)
        {
            InstallationItem? completedItem = null;
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                if (_globalSession.CurrentProcessingItem != null)
                {
                    _globalSession.CurrentProcessingItem.Status = ItemStatus.Failed;
                    completedItem = _globalSession.CurrentProcessingItem;
                    _globalSession.CurrentProcessingItem.ErrorMessage = errorMessage;
                    _globalSession.CurrentProcessingItem.CompletedTime = DateTime.UtcNow;
                    _logger.LogError($"Failed item: {_globalSession.CurrentProcessingItem.DisplayName} - {errorMessage}");
                    _globalSession.CurrentProcessingItem = null;
                }

                _logger.LogInformation("Continuing to process next item after failure");
            }
            if (completedItem != null)
                await NotifyInstallProgress(completedItem, 0);

            await ProcessNextItemIfPossibleAsync();
        }

        public Task<List<InstallationItem>> GetAllItemsAsync()
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    return Task.FromResult(new List<InstallationItem>());

                var allItems = new List<InstallationItem>(_globalSession.InstallationItemList);

                if (_globalSession.CurrentProcessingItem != null &&
                    !allItems.Contains(_globalSession.CurrentProcessingItem))
                {
                    allItems.Add(_globalSession.CurrentProcessingItem);
                }

                return Task.FromResult(allItems);
            }
        }

        public Task<InstallationSession?> GetGlobalSessionAsync()
        {
            lock (_lockObject)
                return Task.FromResult(_globalSession);
        }

        public Task<bool> HasActiveSessionAsync()
        {
            lock (_lockObject)
                return Task.FromResult(_globalSession != null);
        }

        public Task ClearGlobalSessionAsync()
        {
            lock (_lockObject)
            {
                if (_globalSession != null)
                {
                    _logger.LogInformation($"CLEAR global session: {_globalSession.SessionId}");
                    _globalSession = null;
                }
                return Task.CompletedTask;
            }
        }

        private async Task ProcessNextItemIfPossibleAsync()
        {
            InstallationItem? nextItem = null;

            lock (_lockObject)
            {
                if (_globalSession == null || !_globalSession.CanProcessNext())
                {
                    _logger.LogInformation($"Process Next Item If Possible {!_globalSession?.CanProcessNext()}");
                    return;
                }


                nextItem = _globalSession.InstallationItemList
                    .FirstOrDefault(x => x.Status == ItemStatus.Pending);

                if (nextItem != null)
                {
                    nextItem.Status = ItemStatus.Installing;
                    nextItem.StartedTime = DateTime.UtcNow;
                    _globalSession.CurrentProcessingItem = nextItem;
                    _logger.LogInformation($"Started processing: {nextItem.DisplayName}");
                }
                else
                {
                    if (_globalSession.IsAllItemsCompleted())
                    {
                        _globalSession.Status = SessionStatus.Completed;
                        _logger.LogInformation("All items completed. Global session finished.");
                    }
                    else
                    {
                        _logger.LogInformation("No pending items to process at the moment.");
                    }
                }
            }

            if (nextItem != null)
                await ExecuteInstallItemAsync(nextItem);
        }

        private async Task ExecuteInstallItemAsync(InstallationItem item)
        {
            _backgroundService.RunInBackground("installing", async () =>
            {
                try
                {
                    _logger.LogInformation($"Executing scripts for: {item.DisplayName} at {item.ScriptFolderPath}");

                    if (!Directory.Exists(item.ScriptFolderPath))
                        throw new DirectoryNotFoundException($"Script folder not found: {item.ScriptFolderPath}");

                    var availableScripts = PowerShellHelper.GetAvailableScripts(item.ScriptFolderPath);
                    _logger.LogInformation($"Available scripts for {item.DisplayName}: {string.Join(", ", availableScripts)}");

                    bool success = await PowerShellHelper.RunScriptSequenceAsync(item.ScriptFolderPath, _logger);

                    _logger.LogInformation("Is installed successful: " + success);

                    // Simulate script execution for demo purposes, timeout 20s
                    if (success)
                    {
                        _logger.LogInformation($"Successfully executed all scripts for: {item.DisplayName}");
                        await CompleteCurrentItemAndProcessNextAsync();
                    }
                    else
                    {
                        throw new Exception($"One or more scripts failed for {item.DisplayName}");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing scripts for {item.DisplayName}: {ex.Message}");
                    await FailCurrentItemAsync(ex.Message);
                }
            });
            await Task.CompletedTask;
        }

        private string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win32_amd64";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "ubuntu_amd64";
            else
                throw new PlatformNotSupportedException("Unsupported platform");
        }

        public Task StopProcessingAsync()
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                _globalSession.IsManuallyPaused = true;
                _logger.LogInformation("Processing stopped manually. Current item will finish, then queue will pause.");
                return Task.CompletedTask;
            }
        }

        public async Task ContinueProcessingAsync()
        {
            lock (_lockObject)
            {
                if (_globalSession == null)
                    throw new InvalidOperationException("No global session exists.");

                _globalSession.IsManuallyPaused = false;
                _logger.LogInformation("Processing resumed manually.");
            }
            await ProcessNextItemIfPossibleAsync();
        }

        private async Task NotifyInstallProgress(InstallationItem item, int installProgress)
        {
            _logger.LogInformation("[Notify] Send install progress for client");
            var statusUpdate = new
            {
                Id = item.ItemId,
                Version = item.Version,
                InstallProgress = installProgress,
                Status = item.Status.ToString()
            };

            await _hubContext.Clients.All.SendAsync("InstallCompleted", statusUpdate);
        }

        public async Task<object> ControlInstall(string action)
        {
            _logger.LogInformation($"[ControlInstall] Action received: {action}");

            // Check if there's an active session

            string responseMessage = String.Empty;

            switch (action.ToLowerInvariant())
            {
                case "stop":
                    await StopProcessingAsync();
                    responseMessage = "Install process stopped. Current item will finish, then queue will pause.";
                    _logger.LogInformation($"[ControlInstall] Stopped processing");
                    break;

                case "continue":
                    await ContinueProcessingAsync();
                    responseMessage = "Install process resumed. Processing will continue with pending items.";
                    _logger.LogInformation($"[ControlInstall] Resumed processing");
                    break;

                default:
                    responseMessage = $"Unknown action: {action}. Supported actions: 'Stop', 'Continue'";
                    _logger.LogInformation($"[ControlInstall] Unknown action: {action}");
                    break;
            }

            // Get current status after action
            var session = await GetGlobalSessionAsync();
            var queueItems = await GetAllItemsAsync();

            var controlResponse = new
            {
                Action = action,
                Result = responseMessage,
                SessionStatus = session?.Status.ToString(),
                IsManuallyPaused = session?.IsManuallyPaused,
                IsClientConnected = session?.IsClientConnected,
                CurrentProcessing = session?.CurrentProcessingItem?.DisplayName ?? "None",
                QueueItems = queueItems.Select(x => new
                {
                    x.DisplayName,
                    x.Status,
                    x.StartedTime,
                    x.CompletedTime,
                    x.ErrorMessage
                }).ToList(),
                PendingItems = queueItems.Count(x => x.Status == ItemStatus.Pending),
                ProcessingItems = queueItems.Count(x => x.Status == ItemStatus.Installing),
                CompletedItems = queueItems.Count(x => x.Status == ItemStatus.Completed),
                FailedItems = queueItems.Count(x => x.Status == ItemStatus.Failed)
            };

            return controlResponse;
        }

        public async Task<SessionReportStatus> GetSessionStatus(InstallationSession session)
        {
            var itemList = await GetAllItemsAsync();

            var statusResponse = new SessionReportStatus
            {
                SessionId = session.SessionId,
                SessionStatus = session.Status.ToString(),
                ItemList = itemList.Select(x => new ProgressStatus
                {
                    Id = x.ItemId,
                    Status = x.Status.ToString(),
                    Version = x.Version
                }).ToList(),
                TotalItems = itemList.Count,
                CompletedItems = itemList.Count(x => x.Status == ItemStatus.Completed),
                FailedItems = itemList.Count(x => x.Status == ItemStatus.Failed),
                PendingItems = itemList.Count(x => x.Status == ItemStatus.Pending),
            };
            return statusResponse;
        }
    }
}