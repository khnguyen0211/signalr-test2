using System.Diagnostics;
using Application.Constants;
using Application.Services.Interfaces;

namespace Application.Helpers
{
    public class PowerShellHelper
    {
        public static string ExecutePowerShellScript(string script)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-ExecutionPolicy Bypass -NoProfile -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new Exception(Messages.ShellExecution.ProcessStartFailed);
                }
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (string.IsNullOrWhiteSpace(output))
                {
                    throw new Exception(Messages.ShellExecution.EmptyOutput);
                }

                return output;
            }
        }

        public static async Task RunScriptAsync(string scriptPath, string workingDirectory, string scriptExecutor, bool isWindows)
        {
            var tcs = new TaskCompletionSource<bool>();
            var process = new Process();

            process.StartInfo.WorkingDirectory = workingDirectory;

            if (isWindows)
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
                //process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = true;
            }
            else // Linux
            {
                process.StartInfo.FileName = scriptExecutor;
                process.StartInfo.Arguments = $"-c \"chmod +x '{scriptPath}' && '{scriptPath}'\"";
                process.StartInfo.UseShellExecute = true;
            }

            process.EnableRaisingEvents = true;

            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0)
                {
                    tcs.SetResult(true);
                    Console.WriteLine($"Script '{scriptPath}' exited successfully.");
                }
                else
                {
                    tcs.SetResult(false);
                    Console.WriteLine($"Script '{scriptPath}' exited with error code: {process.ExitCode}");
                }
                process.Dispose();
            };


            try
            {
                process.Start();
                Console.WriteLine($"Script '{scriptPath}' started with {scriptExecutor} (UAC will prompt on Windows).");

                if (!await tcs.Task)
                {
                    throw new Exception($"Script '{Path.GetFileName(scriptPath)}' failed with exit code {process.ExitCode}. User might have denied UAC (on Windows) or script encountered an error.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start script '{scriptPath}': {ex.Message}");
                throw;
            }
        }

        public static async Task<bool> RunScriptFileAsync(string scriptFilePath, string workingDirectory, ILoggerService logger)
        {
            if (!File.Exists(scriptFilePath))
            {
                logger.LogError($"Script file not found: {scriptFilePath}");
                return false;
            }

            var fileName = Path.GetFileName(scriptFilePath);
            logger.LogInformation($"Starting PowerShell script: {fileName}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{scriptFilePath}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using var process = new Process { StartInfo = processStartInfo };

                // Capture output and error streams
                var outputBuilder = new System.Text.StringBuilder();
                var errorBuilder = new System.Text.StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        outputBuilder.AppendLine(args.Data);
                        logger.LogInformation($"[{fileName}] {args.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        errorBuilder.AppendLine(args.Data);
                        logger.LogError($"[{fileName}] ERROR: {args.Data}");
                    }
                };

                process.Start();

                // Begin reading output streams
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for completion
                await process.WaitForExitAsync();

                var exitCode = process.ExitCode;
                var hasErrors = errorBuilder.Length > 0;

                if (exitCode == 0 && !hasErrors)
                {
                    logger.LogInformation($"Script completed successfully: {fileName} (Exit code: {exitCode})");
                    return true;
                }
                else
                {
                    logger.LogError($"Script failed: {fileName} (Exit code: {exitCode})");
                    if (hasErrors)
                    {
                        logger.LogError($"Error output: {errorBuilder}");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception running script {fileName}: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> RunScriptSequenceAsync(string scriptFolderPath, ILoggerService logger)
        {
            if (!Directory.Exists(scriptFolderPath))
            {
                logger.LogError($"Script folder not found: {scriptFolderPath}");
                return false;
            }

            var scriptSequence = new[]
            {
                "pre-config.ps1",
                "install.ps1",
                "post-install.ps1",
            };

            logger.LogInformation($"Starting script sequence in folder: {scriptFolderPath}");

            foreach (var scriptFileName in scriptSequence)
            {
                var scriptPath = Path.Combine(scriptFolderPath, scriptFileName);

                if (!File.Exists(scriptPath))
                {
                    logger.LogInformation($"Optional script not found, skipping: {scriptFileName}");
                    continue;
                }

                logger.LogInformation($"Executing script: {scriptFileName}");

                bool success = await RunScriptFileAsync(scriptPath, scriptFolderPath, logger);

                if (!success)
                {
                    logger.LogError($"Script sequence failed at: {scriptFileName}");
                    return false;
                }

                logger.LogInformation($"Script completed: {scriptFileName}");
            }

            logger.LogInformation("All scripts in sequence completed successfully");
            return true;
        }

        public static List<string> GetAvailableScripts(string scriptFolderPath)
        {
            if (!Directory.Exists(scriptFolderPath))
            {
                return new List<string>();
            }

            return Directory.GetFiles(scriptFolderPath, "*.ps1")
                            .Select(Path.GetFileName)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Cast<string>()
                            .ToList();
        }

    }
}
