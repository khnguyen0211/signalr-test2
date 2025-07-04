using Application.Constants;
using Application.Hubs;
using Application.Services.Implementations;
using Application.Services.Interfaces;
using Serilog;

namespace Application.Configurations
{
    public class StartupConfiguration
    {
        private const string AllowedPolicy = "AllowFrontend";
        public static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Configure Kestrel Server
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(SocketConfigs.ServerConfigs.ServerPort);
            });

            // Configure logging
            builder.Logging.AddConsole();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(AllowedPolicy, policy =>
                {
                    policy
                        .WithOrigins(SocketConfigs.ServerConfigs.ClientHost)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Register SignalR
            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 52428800; // Maximum 50MB
            });
        }

        public static void ConfigureApplication(WebApplication webApplication)
        {
            // Middle-ware pipeline
            webApplication.UseRouting();
            webApplication.UseCors(AllowedPolicy);
            // Map SignalR hubs
            webApplication.MapHub<BootstrapHub>("/bootstrapHub");
        }

        public static void StartApplication(WebApplicationBuilder builder)
        {
            LoggerConfiguration loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration);

            if (builder.Configuration["LogFilePath"] != null)
            {
                loggerConfig = loggerConfig.WriteTo.File(Path.Join(builder.Configuration["LogFilePath"], "log.txt"), rollingInterval: RollingInterval.Day);
            }
            builder.Services.AddSingleton<ILoggerService>(new LoggerService(loggerConfig.WriteTo.Console().CreateLogger()));
            builder.Services.AddSingleton<IBootstrapService, BootstrapService>();
            builder.Services.AddSingleton<IInstallationService, InstallationService>();
            builder.Services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();
            builder.Host.UseWindowsService();

            ConfigureServices(builder);
            var app = builder.Build();
            ConfigureApplication(app);
            app.Run();
        }

    }
}
