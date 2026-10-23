// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure
{
    using System;

    internal static class SchemaUrls
    {
        public static string Get(Version semanticConventionsVersion)
            => $"https://opentelemetry.io/schemas/{semanticConventionsVersion.ToString(3)}";
    }
}
