using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core.Handlers
{
    public interface IProviderCommandHandler
    {
        public bool CanHandle(IProviderCommand providerCommand)
        {
            if (providerCommand is null)
                throw new ArgumentNullException(nameof(providerCommand));

            return typeof(IProviderCommandHandler<>)
                .MakeGenericType(providerCommand.GetType())
                .IsAssignableFrom(GetType());
        }

        public Task<ICommandResult> HandleAsync(IProviderCommand providerCommand, IDurableClient durableClient = null)
        {
            if (providerCommand is null)
                throw new ArgumentNullException(nameof(providerCommand));

            if (CanHandle(providerCommand))
            {
                var handleMethod = typeof(IProviderCommandHandler<>)
                    .MakeGenericType(providerCommand.GetType())
                    .GetMethod(nameof(HandleAsync), new Type[] { providerCommand.GetType(), typeof(IDurableClient) });

                return (Task<ICommandResult>)handleMethod
                    .Invoke(this, new object[] { providerCommand, durableClient });
            }

            throw new NotImplementedException($"Missing provider command handler implementation IProviderCommandHandler<{providerCommand.GetType().Name}> at {GetType()}");
        }
    }

    public interface IProviderCommandHandler<T> : IProviderCommandHandler
        where T : class, IProviderCommand
    {
        Task<ICommandResult> HandleAsync(T providerCommand, IDurableClient durableClient = null);
    }
}
