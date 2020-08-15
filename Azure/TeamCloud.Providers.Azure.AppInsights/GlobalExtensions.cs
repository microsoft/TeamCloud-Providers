﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Model.Data.Core;

namespace TeamCloud.Providers.Azure.AppInsights
{
    internal static class GlobalExtensions
    {
        internal static Guid ToRoleDefinitionId(this ProjectUserRole projectUserRole) => projectUserRole switch
        {
            ProjectUserRole.Owner => AzureRoleDefinition.Contributor,
            ProjectUserRole.Member => AzureRoleDefinition.Reader,
            _ => Guid.Empty
        };
    }
}
