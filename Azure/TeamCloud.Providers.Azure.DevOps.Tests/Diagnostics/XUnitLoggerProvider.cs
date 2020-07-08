/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

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

        void System.IDisposable.Dispose()
        { }
    }

}
