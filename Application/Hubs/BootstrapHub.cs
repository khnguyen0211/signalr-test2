using Application.Constants;
using Application.Enums;
using Application.Helpers;
using Application.Models.FileUploads;
using Application.Models.Installations;
using Application.Models.SystemInformation;
using Application.Services.Implementations;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs
{
    public class BootstrapHub : Hub
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly IEncryptionService _encryptionService;
        private readonly IConnectionManagerService _connectionManager;
        private readonly ISystemInformationService _systemInformationService;
        private readonly ILoggerService _logger;
        private readonly IBootstrapService _bootstrapStageExecutorService;
        private readonly IExtractionService _extractionService;
        private readonly IInstallationService _installQueueService;


        public BootstrapHub(ILoggerService logger, IBootstrapService bootstrapStageExecutorService, IInstallationService installQueueService)
        {
            _fileUploadService = FileUploadService.Instance;
            _encryptionService = EncryptionService.Instance;
            _systemInformationService = SystemInformationService.Instance;
            _connectionManager = ConnectionManagerService.Instance;
            _extractionService = ExtractionService.Instance;
            _logger = logger;
            _bootstrapStageExecutorService = bootstrapStageExecutorService;
            _installQueueService = installQueueService;
        }

        public async Task WebToBootstrap(string message)
        {
            try
            {
                // Verify this connection is registered and active
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }
                _logger.LogInformation($"[Server] Message from {Context.ConnectionId}: {message}");
                // Only send to the current active connection (single client)
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SendMessage Error] {ex.Message}");
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }

        public async Task StartUpload(UploadMetaData metaData)
        {
            try
            {
                // Verify this connection is registered and active
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }
                var session = _fileUploadService.StartUploadAsync(Context.ConnectionId, metaData);
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, $"SessionId: {session.SessionId}");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }

        public async Task UploadChunk(byte[] encryptedChunk, int chunkNumber)
        {
            try
            {
                // Verify this connection is registered and active
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }

                //TODO: Handle upload chunk
                await _fileUploadService.ProcessChunkAsync(Context.ConnectionId, encryptedChunk, chunkNumber);
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.UploadingChunk);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }

        public async Task EndUpload()
        {
            try
            {
                // Verify this connection is registered and active
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }

                //TODO: Handle end upload
                await _fileUploadService.EndUploadAsync(Context.ConnectionId);
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.UploadSuccessful);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }

        public async Task GetWindowsSystemInfo()
        {
            try
            {
                // Verify this connection is registered and active
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }

                SystemInfo sysinfo = _systemInformationService.GetWindowsSystemInfo();
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.ReadSystemInfo, sysinfo);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }

        public async Task StartInstall(string requestedVersion)
        {
            string? bundlePath = string.Empty;
            try
            {
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }

                string[] files = Directory.GetFiles(Path.Combine(Path.GetTempPath(), HashHelper.GetHashString(Context.ConnectionId)), "*" + ".bundle", SearchOption.TopDirectoryOnly);

                if (files.Length > 0)
                {
                    bundlePath = files.First();
                }

                await _bootstrapStageExecutorService.ExecuteBootstrapStagesAsync(bundlePath, requestedVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during installation initiation in Hub: {ex.Message}");
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.ReceiveInstallationStatus, new InstallationStatus
                {
                    StageName = "Error",
                    Message = $"Installation initiation failed: {ex.Message}",
                    HasError = true,
                    ErrorMessage = ex.Message
                });
            }
            finally
            {
                // Clean up the temporary zip file if it was created
                if (bundlePath != null && File.Exists(bundlePath))
                {
                    try
                    {
                        File.Delete(bundlePath);
                        _logger.LogInformation($"Temporary zip file deleted: {bundlePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Could not delete temporary zip file {bundlePath}: {ex.Message}");
                    }
                }
            }
        }

        public async Task IsInstalledApplication(string applicationName)
        {
            await Clients.Caller.SendAsync(SocketConfigs.SocketActions.InitializedApplicationStatus, InstallationStatusEnum.Checking);

            try
            {
                var isInstalled = _bootstrapStageExecutorService.CheckInstallationApplicationStatusByAppNameAsync(applicationName);
                if (isInstalled)
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.InitializedApplicationStatus, InstallationStatusEnum.Installed);
                }
                else
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.InitializedApplicationStatus, InstallationStatusEnum.NotInstalled);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
            }
        }

        public async Task GetInstalledApplicationVersion(string applicationName)
        {
            string version = string.Empty;
            try
            {
                version = _bootstrapStageExecutorService.GetInstalledApplicationVersion(applicationName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
            }

            await Clients.Caller.SendAsync(SocketConfigs.SocketActions.InstalledApplicationVersion, version);
        }


        public override async Task OnConnectedAsync()
        {
            try
            {
                // Try to register this connection
                bool isAccepted = _connectionManager.TryRegisterConnection(Context.ConnectionId);

                if (!isAccepted)
                {
                    // Reject the connection
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.Rejected);

                    // Forcefully close the connection
                    Context.Abort();
                    return;
                }
                _logger.LogInformation($"[Connected] {Context.ConnectionId}");
                // Generate encryption key for this connection
                string encryptionKey = _encryptionService.GenerateEncryptionKey(Context.ConnectionId);
                // Associate the connection with the install queue service
                await _installQueueService.SetupConnectionId(Context.ConnectionId);

                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.SetEncryptionKey, encryptionKey);
                // Connection accepted
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                string connectionId = Context.ConnectionId;
                _logger.LogInformation($"[Disconnected] {connectionId}");

                // Clean up all resources for this connection
                bool isUnregistered = _connectionManager.UnregisterConnection(connectionId);
                _logger.LogInformation($"[Unregistered] {isUnregistered}");
                bool isRemoved = _encryptionService.RemoveEncryptionKey(connectionId);
                _logger.LogInformation($"[RemovedKey] {isRemoved}");
                // Disconnect from the install queue service
                await _installQueueService.DisconnectAsync(connectionId);

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }

        }

        public async Task Install(List<InstallApplication> installApplicationList)
        {
            try
            {
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }
                var response = await _installQueueService.SequentialInstallation(Context.ConnectionId, installApplicationList);
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb,
                    $"Install Status: {Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }

        }

        public async Task ControlInstall(string action)
        {
            try
            {
                // Verify connection
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }

                _logger.LogInformation($"[ControlInstall] Action received: {action} from connection: {Context.ConnectionId}");

                // Check if there's an active session
                if (!await _installQueueService.HasActiveSessionAsync())
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb,
                        "No active install session found to control");
                    return;
                }

                var controlResponse = await _installQueueService.ControlInstall(action);
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb,
                    $"Control Install Response: {Newtonsoft.Json.JsonConvert.SerializeObject(controlResponse, Newtonsoft.Json.Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ControlInstall Error] {ex.Message}");
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb,
                    $"Control install failed: {ex.Message}");
            }
        }

        public async Task GetSessionStatus()
        {
            try
            {
                if (!_connectionManager.IsConnectionActive(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, Messages.Connection.NotAuthorized);
                    return;
                }
                _logger.LogInformation("Get installation status");
                var session = await _installQueueService.GetGlobalSessionAsync();
                if (session == null)
                {
                    await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb,
                        "No active install session found");
                    return;
                }

                var queueStatuses = await _installQueueService.GetSessionStatus(session);
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.ReportSessionStatus,
                     Newtonsoft.Json.JsonConvert.SerializeObject(queueStatuses, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetInstallStatus Error] {ex.Message}");
                await Clients.Caller.SendAsync(SocketConfigs.SocketActions.BootstrapToWeb, ex.Message);
            }
        }
    }
}
