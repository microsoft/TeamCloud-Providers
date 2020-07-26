/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Testing.Diagnostics
{
    public sealed class XUnitLogger : ILogger
    {
        public static ILogger Create<T>(ITestOutputHelper outputHelper)
            => Create(typeof(T), outputHelper);

        public static ILogger Create(Type type, ITestOutputHelper outputHelper)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (outputHelper is null)
                throw new ArgumentNullException(nameof(outputHelper));

            using var loggerFactory = new LoggerFactory();
            using var loggerProvider = new XUnitLoggerProvider(outputHelper);

            loggerFactory.AddProvider(loggerProvider);

            return loggerFactory.CreateLogger(type ?? throw new ArgumentNullException(nameof(type)));
        }

        private readonly ITestOutputHelper testOutputHelper;
        private readonly string categoryName;

        public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            this.testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            testOutputHelper.WriteLine($"{categoryName} - {logLevel} [{eventId}] - {formatter?.Invoke(state, exception)}".Trim());

            if (exception != null)
                testOutputHelper.WriteLine(exception.ToString());
        }
    }
}
