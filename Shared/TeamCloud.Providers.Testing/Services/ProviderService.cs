/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using TeamCloud.Http;
using Xunit;

namespace TeamCloud.Providers.Testing.Services
{
    public sealed class ProviderService : IAsyncLifetime
    {
        private const string WindowsNewLine = "\r\n";
        private const int FunctionHostTimeout = 60;

        private readonly Lazy<Process> hostProcess;
        private readonly List<string> hostErrors = new List<string>();

        private static bool WaitFor(Func<bool> predicate, int timeout)
        {
            var absoluteTimeout = DateTime.UtcNow.AddMilliseconds(timeout);

            while (!predicate())
            {
                if (DateTime.UtcNow < absoluteTimeout)
                    Thread.Sleep(Math.Min(timeout / 10, 1000));
                else
                    return false;
            }

            return true;
        }

        private static string SanitizeOutput(string output)
        {
            output = output.Trim();

            output = Regex.Replace(output, @"[\\]{2,}", @"\\");
            output = Regex.Replace(output, @"(?<!\r)\n", WindowsNewLine);
            output = Regex.Replace(output, @"[\r\n]{2,}", WindowsNewLine);

            return output.TrimEnd('\r', '\n').Trim();
        }

        private static string[] GetToolPaths(string name)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = "where",
                Arguments = name
            });

            process.WaitForExit();

            var output = process.StandardOutput
                .ReadToEnd();

            return SanitizeOutput(output)
                .Split(Environment.NewLine);
        }

        private static void ResetStorageEmulator()
        {
            var path = GetToolPaths("azurestorageemulator").FirstOrDefault();

            if (!string.IsNullOrEmpty(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    CreateNoWindow = !Debugger.IsAttached,
                    UseShellExecute = true,
                    FileName = path,
                    Arguments = "init -forcecreate"

                }).WaitForExit(true);

                Process.Start(new ProcessStartInfo
                {
                    CreateNoWindow = !Debugger.IsAttached,
                    UseShellExecute = true,
                    FileName = path,
                    Arguments = "start"

                }).WaitForExit(true);
            }
        }

        private static async Task<string> GetPublicUrlAsync(string localUrl)
        {
            if ((localUrl?.Contains("localhost", StringComparison.OrdinalIgnoreCase) ?? false) && Process.GetProcessesByName("ngrok").Any())
            {
                var response = await "http://localhost:4040/api/tunnels"
                    .AllowAnyHttpStatus()
                    .GetAsync()
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content
                        .ReadAsJsonAsync()
                        .ConfigureAwait(false);

                    var exposedAddr = json
                        .SelectTokens("$.tunnels[?(@.proto == 'https')].config.addr")
                        .Select(token => token.ToString())
                        .Where(addr => addr.Equals(localUrl, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    return json
                        .SelectToken($"$.tunnels[?(@.proto == 'https' && @.config.addr =='{exposedAddr}')].public_url")?
                        .ToString() ?? localUrl;
                }
            }

            return localUrl;
        }

        private static string GetFunctionHostPath()
        {
            var path = GetToolPaths("npm")
                .First(p => p.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase));

            using var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = path,
                Arguments = "root -g"
            });

            process.WaitForExit();

            var output = process.StandardOutput
                .ReadToEnd();

            var root = SanitizeOutput(output);

            return Directory
                .GetFiles(root, "func.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
        }

        private static string GetFunctionAppRoot()
        {
            var binPath = Directory.GetCurrentDirectory();
            var funcRoot = Path.Combine(binPath, "FunctionHost");

            return Directory.CreateDirectory(funcRoot).FullName;
        }

        private static IEnumerable<string> GetFunctionAppPaths()
        {
            var funcRoot = GetFunctionAppRoot();

            return Directory.Exists(funcRoot)
                ? Directory.GetDirectories(funcRoot)
                : Enumerable.Empty<string>();
        }

        public ProviderService()
        {
            hostProcess = new Lazy<Process>(() =>
            {
                ResetStorageEmulator();

                var baseUrl = OrchestratorService.Instance.BaseUrl;
                Debug.WriteLine($"Callback host listening on {baseUrl}");

                var hostPath = GetFunctionHostPath();
                Debug.WriteLine($"Function host found: {hostPath}");

                var appPaths = GetFunctionAppPaths();
                Debug.WriteLine($"Function apps found: {string.Join(';', appPaths)}");

                var process = Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = hostPath,
                    Arguments = "host start",
                    WorkingDirectory = appPaths.SingleOrDefault() ?? throw new DirectoryNotFoundException($"Missing function app folder under '{GetFunctionAppRoot()}'")
                });

                process.BeginOutputReadLine();
                process.OutputDataReceived += CaptureOutput;

                process.BeginErrorReadLine();
                process.ErrorDataReceived += CaptureError;

                try
                {
                    var timeout = FunctionHostTimeout * 1000 * (Debugger.IsAttached ? 5 : 1);

                    if (!WaitFor(() => process.HasExited || !string.IsNullOrEmpty(BaseUrl), timeout))
                    {
                        throw new TimeoutException($"Failed to start function host within {FunctionHostTimeout} sec.");
                    }
                    else if (process.HasExited)
                    {
                        throw new Exception($"Function host failed to start: {string.Join(" / ", hostErrors)}");
                    }
                    else
                    {
                        process.PriorityBoostEnabled = true;
                    }
                }
                catch
                {
                    process.Dispose();

                    throw;
                }

                return process;
            });
        }

        private void CaptureOutput(object sender, DataReceivedEventArgs eventArgs)
        {
            var data = eventArgs?.Data ?? string.Empty;

            var match = Regex.Match(data, @"(?:https?):\/\/0.0.0.0:\d+");

            if (match.Success)
                BaseUrl = match.Value.Replace("0.0.0.0", "localhost");

            Debug.WriteLine(data);
        }

        private void CaptureError(object sender, DataReceivedEventArgs eventArgs)
        {
            var data = eventArgs?.Data ?? string.Empty;

            if (!string.IsNullOrEmpty(data))
            {
                var errors = SanitizeOutput(data)
                    .Split(WindowsNewLine);

                hostErrors.AddRange(errors);
            }

            Debug.WriteLine(data);
        }

        public DateTime? Started
            => hostProcess.IsValueCreated ? hostProcess.Value.StartTime : default;

        public string BaseUrl { get; private set; }

        async Task IAsyncLifetime.InitializeAsync()
        {
            if (!hostProcess.Value.HasExited)
            {
                BaseUrl = await GetPublicUrlAsync(BaseUrl)
                    .ConfigureAwait(false);
            }

            await OrchestratorService.Instance
                .StartAsync()
                .ConfigureAwait(false);
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            if (hostProcess.IsValueCreated)
            {
#pragma warning disable CA1031 // Do not catch general exception types

                try
                {
                    hostProcess.Value.Kill(true);
                }
                catch
                {
                    // swallow
                }

                try
                {
                    hostProcess.Value.Dispose();
                }
                catch
                {
                    // swallow
                }

#pragma warning restore CA1031 // Do not catch general exception types
            }

            return Task.CompletedTask;
        }
    }
}
