# WarpBootstrap

A SignalR-based file upload and script execution service built with ASP.NET 8.0. This application provides secure file upload capabilities with encryption, archive extraction, and automated script execution.

## Prerequisites

- **Operating System**: Windows 10/11 or Windows Server 2019+
- **.NET 8.0 SDK**: Download from [Microsoft .NET Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022**: Community, Professional, or Enterprise edition
- **PowerShell**: Version 5.1 or later (included with Windows)

## Development Environment Setup

### 1. Install Visual Studio 2022

1. Download Visual Studio 2022 from [Visual Studio Downloads](https://visualstudio.microsoft.com/downloads/)
2. During installation, ensure you select the following workloads:
   - **ASP.NET and web development**
   - **.NET desktop development**
3. In the Individual Components tab, verify these components are selected:
   - .NET 8.0 Runtime
   - .NET 8.0 SDK
   - NuGet package manager
   - Git for Windows (optional but recommended)

### 2. Clone and Open the Project

```bash
git clone <repository-url>
cd WarpBootstrap
```

Open the solution file `WarpBootstrap.sln` in Visual Studio 2022.

### 3. Restore NuGet Packages

Visual Studio should automatically restore packages. If not, manually restore:

```bash
dotnet restore
```

## SSL Certificate Configuration

### 1. Generate Development Certificate

Open PowerShell or Command Prompt as Administrator and run:

```bash
dotnet dev-certs https --export-path ./mycert.pfx --password mypassword
```

This creates a certificate file `mycert.pfx` in your current directory.

### 2. Configure Certificate Path

Update your `appsettings.Development.json` file with the correct certificate path:

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "ClientHost": "http://127.0.0.1:5500",
    "ServerPort": 5001,
    "CertPath": "C:\\path\\to\\your\\certificate.pfx",
    "Password": "password",
}
```

**Important**: Replace `C:\\path\\to\\your\\mycert.pfx` with the actual full path to your certificate file.

### 3. Trust the Development Certificate

```bash
dotnet dev-certs https --trust
```

Click "Yes" when prompted to install the certificate.

## Configuration

### Environment Configuration

The application uses several configuration settings that can be modified in `appsettings.Development.json`:

- **ClientHost**: The allowed client origin for CORS (default: `http://127.0.0.1:5500`)
- **ServerPort**: The HTTPS port for the application (default: `5001`)
- **CertPath**: Full path to your SSL certificate file
- **Password**: Password for the SSL certificate

## Running the Application

### Development Mode

#### Using Visual Studio 2022

1. Set the startup project to `WarpBootstrap`
2. Select the `https` launch profile
3. Press F5 or click the "Start" button

#### Using Command Line

```bash
dotnet run --project WarpBootstrap.csproj --launch-profile https
```

The application will start and listen on:
- HTTPS: `https://localhost:7106`
- HTTP: `http://localhost:5096`
- Custom HTTPS: `https://localhost:5001` (configured in Kestrel)

### Production Mode

```bash
dotnet run --configuration Release --environment Production
```

## Publishing the Application

### Self-Contained Deployment

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

## Troubleshooting

### Certificate Issues

If you encounter SSL certificate errors:

1. Ensure the certificate path is correct and accessible
2. Verify the certificate password matches the configuration
3. Check that the certificate hasn't expired
4. Try regenerating the development certificate

### Port Conflicts

If port 5001 is already in use:

1. Change the `ServerPort` in `appsettings.Development.json`
2. Update the Kestrel configuration in `Program.cs`
3. Restart the application

## API Endpoints

The application primarily uses SignalR for communication, but also exposes:

- **SignalR Hub**: `/bootstrapHub` - Main communication endpoint
- **Health Check**: Available through standard ASP.NET Core health checks

## Dependencies

- **SharpCompress** (0.40.0): Archive extraction
- **System.Security.Cryptography.Cng** (5.0.0): Cryptographic operations
- **Microsoft.AspNetCore.SignalR**: Real-time communication

## Development Notes

- The application uses singleton pattern for service instances
- File uploads are processed in encrypted chunks
- Temporary files are automatically cleaned up on disconnection
- Script execution supports Windows batch files and PowerShell scripts
- Archive extraction preserves file structure and metadata
