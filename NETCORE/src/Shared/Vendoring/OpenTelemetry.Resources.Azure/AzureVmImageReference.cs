// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#nullable enable

namespace Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure
{
    using System.Text.Json.Serialization;

    internal sealed class AzureVmImageReference
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("offer")]
        public string? Offer { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }
    }
}
