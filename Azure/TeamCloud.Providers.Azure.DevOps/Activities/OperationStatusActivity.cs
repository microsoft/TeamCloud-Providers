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
    public sealed class OperationStatusActivity
    {
        private readonly IAuthenticationService authenticationService;

        public OperationStatusActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(OperationStatusActivity))]
        public async Task<OperationStatus> RunActivity(
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

                    return operation.Status;
                }

                return OperationStatus.Failed;
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"{nameof(ProjectCreateActivity)} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
