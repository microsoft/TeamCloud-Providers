using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Orchestration;

namespace TeamCloud.Providers.Core.Activities
{
    public static class ProviderCommandAppSettingActivity
    {
        [FunctionName(nameof(ProviderCommandAppSettingActivity)), RetryOptions(3)]
        public static async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (key, value) = functionContext.GetInput<(string, string)>();

            var appSettings = await FunctionEnvironment
                .GetAppSettingsAsync()
                .ConfigureAwait(false);

            if (value is null)
            {
                appSettings.Remove(key);
            }
            else if (!appSettings.TryGetValue(key, out var existingValue) || existingValue != value)
            {
                appSettings[key] = value;
            }
            else
            {
                return; // app settings unmodified - no need to send an update
            }

            _ = await FunctionEnvironment
                .SetAppSettingsAsync(appSettings)
                .ConfigureAwait(false);
        }
    }
}
