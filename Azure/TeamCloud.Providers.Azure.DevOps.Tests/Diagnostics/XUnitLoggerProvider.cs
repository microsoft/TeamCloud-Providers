using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Azure.DevOps.Diagnostics
{
    public sealed class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper testOutputHelper;

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
            => new XUnitLogger(testOutputHelper, categoryName);

#pragma warning disable CA1063 // Implement IDisposable Correctly

        void System.IDisposable.Dispose()
        { }

#pragma warning restore CA1063 // Implement IDisposable Correctly

    }

}
