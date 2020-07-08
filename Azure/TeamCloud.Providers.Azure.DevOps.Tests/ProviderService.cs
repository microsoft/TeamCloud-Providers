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
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps;

[assembly: FunctionsImport(typeof(Startup))]

namespace TeamCloud.Providers.Azure.DevOps
{
    public sealed class ProviderService : IDisposable
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

        private static IEnumerable<string> GetFunctionAppPaths()
        {
            var binPath = Directory.GetCurrentDirectory(); /* Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);*/
            var funcPath = Path.Combine(binPath, "FunctionHost");

            return Directory.Exists(funcPath)
                ? Directory.GetDirectories(funcPath)
                : Enumerable.Empty<string>();
        }

        public ProviderService()
        {
            hostProcess = new Lazy<Process>(() =>
            {
                var baseUrl = CallbackHost.Instance.GetBaseUrl();
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
                    WorkingDirectory = appPaths.Single() // for now we support just a one hosted function app during testing
                });

                process.BeginOutputReadLine();
                process.OutputDataReceived += this.CaptureOutput;

                process.BeginErrorReadLine();
                process.ErrorDataReceived += this.CaptureError;

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

                        return process;
                    }
                }
                catch
                {
                    process.Dispose();

                    throw;
                }
            });
        }

        ~ProviderService()
        {
            this.Dispose();
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

        public async Task<bool> StartAsync()
        {
            try
            {
                var started = !hostProcess.Value.HasExited;

                if (started)
                {
                    BaseUrl = await GetPublicUrlAsync(BaseUrl)
                        .ConfigureAwait(false);
                }

                return started;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public string BaseUrl { get; private set; }


        public void Dispose()
        {
            if (hostProcess.IsValueCreated && !hostProcess.Value.HasExited)
            {
                try
                {
                    hostProcess.Value.Kill(true);
                }
                catch
                {
                    // swallow
                }
                finally
                {
                    hostProcess.Value.Dispose();
                }
            }
        }
    }
}
