/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using NSubstitute;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Core.Configuration;
using Xunit;

namespace TeamCloud.Providers.Core.Activities
{
    public class ProviderCommandDispatchActivityTests
    {
        [Fact]
        public void GetOrchestration_NoConfiguration()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>());

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestCommand();

            Assert.Throws<NotSupportedException>(() => dispatcher.RunActivity(command));
        }

        [Fact]
        public void GetOrchestration_Exact_Match()
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
        public void GetOrchestration_Exact_Ignored()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(TestInheritedCommand)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestration_Inherited_Match()
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
        public void GetOrchestration_Inherited_Ignored()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(TestCommand)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestration_Interface_Match()
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
        public void GetOrchestration_Interface_Ignore()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(ICommand)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        [Fact]
        public void GetOrchestration_InterfaceGeneric_Match()
        {
            var settings = Substitute.For<IOrchestrationSettings>();

            settings.OrchestrationName.Returns(nameof(TestCommandOrchestration));

            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Orchestrations.Returns(new Dictionary<Type, IOrchestrationSettings>()
            {
                { typeof(ICommand<TestPayload, TestCommandResult>), settings }
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Equal(nameof(TestCommandOrchestration), orchestration);
        }

        [Fact]
        public void GetOrchestration_InterfaceGeneric_Ignore()
        {
            var configuration = Substitute.For<IOrchestrationConfiguration>();

            configuration.Ignored.Returns(new List<Type>()
            {
                typeof(ICommand<TestPayload, TestCommandResult>)
            });

            var dispatcher = new ProviderCommandDispatchActivity(configuration);
            var command = new TestInheritedCommand();
            var orchestration = dispatcher.RunActivity(command);

            Assert.Null(orchestration);
        }

        #region Mocks

        public class TestPayload
        { }

        public class TestCommand : ProviderCommand<TestPayload, TestCommandResult>
        {
            private static readonly User DefaultUser = new User()
            {
                Id = Guid.NewGuid(),
                Role = TeamCloudUserRole.None
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
