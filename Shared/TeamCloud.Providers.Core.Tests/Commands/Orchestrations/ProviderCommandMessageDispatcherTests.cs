using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using Xunit;

namespace TeamCloud.Providers.Core.Commands.Orchestrations
{
    public class ProviderCommandMessageDispatcherTests
    {
        [Fact]
        public void GetOrchestration_NoConfiguration()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, string>());

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestCommand(), new Provider(), "http://localhost/callback");

            Assert.Throws<NotSupportedException>(() => dispatcher.Run(message, NullLogger.Instance));
        }

        [Fact]
        public void GetOrchestration_Exact_Match()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, string>()
            {
                { typeof(TestCommand), nameof(TestCommandOrchestration) }
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestration_Exact_Ignored()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(TestInheritedCommand)
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestration_Inherited_Match()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, string>()
            {
                { typeof(TestCommand), nameof(TestCommandOrchestration) }
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestration_Inherited_Ignored()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(TestCommand)
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestration_Interface_Match()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, string>()
            {
                { typeof(ICommand), nameof(TestCommandOrchestration) }
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestration_Interface_Ignore()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(ICommand)
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestration_InterfaceGeneric_Match()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, string>()
            {
                { typeof(ICommand<TestPayload, TestCommandResult>), nameof(TestCommandOrchestration) }
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestration_InterfaceGeneric_Ignore()
        {
            var configuration = Substitute.For<IConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(ICommand<TestPayload, TestCommandResult>)
            });

            var dispatcher = new ProviderCommandMessageDispatcher(configuration);
            var message = new ProviderCommandMessage(new TestInheritedCommand(), new Provider(), "http://localhost/callback");
            var orchestration = dispatcher.Run(message, NullLogger.Instance);

            Assert.Null(orchestration);
        }
        #region Mocks

        public class TestPayload
        { }

        public class TestCommand : Command<TestPayload, TestCommandResult>
        {
            private static readonly User DefaultUser = new User()
            {
                Id = Guid.NewGuid(),
                Role = "Unknown"
            };

            private static readonly TestPayload DefaultPayload = new TestPayload();

            public TestCommand() : this(DefaultUser, DefaultPayload)
            { }

            public TestCommand(User user, TestPayload payload) : base(user, payload)
            { }
        }

        public class TestCommandResult : CommandResult<TestPayload>
        { }

        public class TestCommandOrchestration
        { }

        public class TestInheritedCommand : TestCommand
        { }

        public class TestInheritedCommandOrchestration
        { }

        #endregion Mocks
    }
}
