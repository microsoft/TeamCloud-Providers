/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Core.Configuration;
using Xunit;

namespace TeamCloud.Providers.Core.Activities
{
    [SuppressMessage("Microsoft.Performance", "CA1812:Avoid Uninstantiated Internal Classes", Justification = "Dynamically instatiated")]
    public class ProviderCommandDispatchActivityTests
    {
        [Fact]
        public void GetOrchestrationNoConfiguration()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>());

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestCommand();

            Assert.Throws<NotSupportedException>(() => dispatcher.RunActivity(command));
        }

        [Fact]
        public void GetOrchestrationExactMatch()
        {
            var settings = Substitute.For<IOrchestrationSettings>();

            settings.OrchestrationName.Returns(nameof(TestCommandOrchestration));

            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>()
            {
                { typeof(TestCommand),  settings }
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestrationExactIgnored()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new HashSet<Type>()
            {
                typeof(TestInheritedCommand)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestrationInheritedMatch()
        {
            var settings = Substitute.For<IOrchestrationSettings>();

            settings.OrchestrationName.Returns(nameof(TestCommandOrchestration));

            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>()
            {
                { typeof(TestCommand),  settings }
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestrationInheritedIgnored()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new HashSet<Type>()
            {
                typeof(TestCommand)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestrationInterfaceMatch()
        {
            var settings = Substitute.For<IOrchestrationSettings>();

            settings.OrchestrationName.Returns(nameof(TestCommandOrchestration));

            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>()
            {
                { typeof(ICommand),  settings }
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestrationInterfaceIgnore()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new HashSet<Type>()
            {
                typeof(ICommand)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestrationInterfaceGenericMatch()
        {
            var settings = Substitute.For<IOrchestrationSettings>();

            settings.OrchestrationName.Returns(nameof(TestCommandOrchestration));

            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>()
            {
                { typeof(IProviderCommand<TestPayload, TestCommandResult>), settings }
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestrationInterfaceGenericIgnore()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new HashSet<Type>()
            {
                typeof(IProviderCommand<TestPayload, TestCommandResult>)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        #region Mocks

        internal class TestPayload
        { }

        internal class TestCommand : ProviderCommand<TestPayload, TestCommandResult>
        {
            private static readonly User DefaultUser = new User()
            {
                Id = Guid.NewGuid().ToString(),
                Role = TeamCloudUserRole.None
            };

            private static readonly TestPayload DefaultPayload = new TestPayload();

            public TestCommand() : this(DefaultUser, DefaultPayload)
            { }

            public TestCommand(User user, TestPayload payload) : base(user, payload)
            { }
        }

        internal class TestCommandResult : CommandResult<TestPayload>
        { }

        internal class TestCommandOrchestration
        { }

        internal class TestInheritedCommand : TestCommand
        { }

        internal class TestInheritedCommandOrchestration
        { }

        #endregion Mocks
    }
}
