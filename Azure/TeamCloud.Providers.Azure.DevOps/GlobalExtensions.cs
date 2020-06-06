using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.VisualStudio.Services.Operations;

namespace TeamCloud.Providers.Azure.DevOps
{
    internal static class GlobalExtensions
    {
        public static Dictionary<string, string[]> ToDictionary(this NameValueCollection collection)
            => collection.Cast<string>().ToDictionary(key => key, key => collection.GetValues(key));


        public static bool TryGetValue(this NameValueCollection collection, string key, out string value)
        {
            value = collection.AllKeys.Contains(key)
                ? collection.Get(key) : default;

            return value != default;
        }

        public static bool IsProgressStatus(this OperationStatus instance)
            => instance == OperationStatus.Queued
            || instance == OperationStatus.InProgress;

        public static bool IsErrorStatus(this OperationStatus instance)
            => instance == OperationStatus.Cancelled
            || instance == OperationStatus.Failed
            || instance == OperationStatus.NotSet;

        public static bool IsFinalStatus(this OperationStatus instance)
            => instance == OperationStatus.Cancelled
            || instance == OperationStatus.Failed
            || instance == OperationStatus.NotSet
            || instance == OperationStatus.Succeeded;

    }
}
