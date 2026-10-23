// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#nullable enable

namespace Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure
{
    using System.Text.Json.Serialization;

    internal sealed class AzureVmStorageProfile
    {
        [JsonPropertyName("imageReference")]
        public AzureVmImageReference? ImageReference { get; set; }
    }
}
