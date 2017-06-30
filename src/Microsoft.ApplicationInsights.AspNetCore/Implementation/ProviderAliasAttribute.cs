// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Controls logger provider alias used for configuration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class ProviderAliasAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderAliasAttribute" /> class.
        /// </summary>
        public ProviderAliasAttribute(string alias)
        {
            Alias = alias;
        }

        /// <summary>
        /// Gets an alias that can be used insted full type name during configuration.
        /// </summary>
        public string Alias { get; }
    }
}