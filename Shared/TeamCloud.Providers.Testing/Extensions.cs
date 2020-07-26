/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamCloud.Model.Commands.Core;
using Xunit;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Testing
{
    public static class Extensions
    {
        internal static void WaitForExit(this Process process, bool dispose)
        {
            try
            {
                process.WaitForExit();
            }
            finally
            {
                if (dispose) process.Dispose();
            }
        }

        internal static Task<T> ReadAsAsync<T>(this HttpContent httpContent, JsonSerializerSettings serializerSettings = null)
            => httpContent.ReadAsAsync<T>(JsonSerializer.CreateDefault(serializerSettings));

        internal static async Task<T> ReadAsAsync<T>(this HttpContent httpContent, JsonSerializer serializer)
        {
            using var stream = await httpContent.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);

            return serializer.Deserialize<T>(jsonReader);
        }

        internal static object SetValue(this JValue instance, object value)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            return instance.Value = value;
        }

        internal static ITest GetTest(this ITestOutputHelper testOutputHelper)
        {
            if (testOutputHelper is null)
                throw new ArgumentNullException(nameof(testOutputHelper));

            var fieldInfo = testOutputHelper.GetType()
                .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);

            return (ITest)fieldInfo.GetValue(testOutputHelper);
        }

        public static Guid ToGuid(this string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (Guid.TryParse(value, out var guid))
                return guid;

            using var algorithm = HashAlgorithm.Create("MD5");

            var buffer = Encoding.UTF8.GetBytes(value);
            var hash = algorithm.ComputeHash(buffer);

            return new Guid(hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShouldHaveRuntimeStatus(this ICommandResult commandResult, CommandRuntimeStatus runtimeStatus)
        {
            if (commandResult is null)
                throw new ArgumentNullException(nameof(commandResult));

            var message = new StringBuilder();

            message.AppendLine($"Runtime Status is not '{runtimeStatus}' but '{commandResult.RuntimeStatus}'");

            foreach (var error in commandResult.Errors)
            {
                if (error == commandResult.Errors.First())
                    message.AppendLine("Errors:");

                message.Append($"- [{error.Severity}] {error.Message}");
            }

            Assert.True(commandResult.RuntimeStatus == runtimeStatus, message.ToString());
        }

    }
}
