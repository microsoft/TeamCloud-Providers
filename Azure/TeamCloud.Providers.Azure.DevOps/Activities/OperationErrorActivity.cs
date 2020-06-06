using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Operations;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class OperationErrorActivity
    {
        private readonly IAuthenticationService authenticationService;

        public OperationErrorActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(OperationErrorActivity))]
        public async Task<string> RunActivity(
            [ActivityTrigger] string operationInstanceId,
            ILogger log)
        {
            try
            {
                var connection = await authenticationService
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                var operationClient = await connection
                    .GetClientAsync<OperationsHttpClient>()
                    .ConfigureAwait(false);

                if (Guid.TryParse(operationInstanceId, out var operationId))
                {
                    var operation = await operationClient
                        .GetOperation(operationId)
                        .ConfigureAwait(false);

                    if (operation.Status == OperationStatus.Succeeded)
                        return null;

                    return operation.ResultMessage;
                }

                return $"Operation ID '{operationInstanceId}' is invalid";
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"{nameof(ProjectCreateActivity)} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
