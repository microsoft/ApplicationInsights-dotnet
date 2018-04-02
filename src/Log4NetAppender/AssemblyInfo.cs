// -----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.ApplicationInsights.Log4NetAppender")]
[assembly: AssemblyDescription("Application Insights Log4Net Appender is a customer appender allowing you to send Log4Net log messages to Application Insights. Application Insights will collect your logs from multiple sources and provide rich powerful search capabilities. This functionality depends on Microsoft Monitoring Agent being installed onto your machine, http://go.microsoft.com/fwlink/?LinkID=328052.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: NeutralResourcesLanguageAttribute("en-US")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("599daa99-7d0b-4cae-81b8-3a73509f2efa")]

[assembly: InternalsVisibleTo("Microsoft.ApplicationInsights.Log4NetAppender.Net45.Tests, PublicKey=" + AssemblyInfo.PublicKey)]
[assembly: InternalsVisibleTo("Microsoft.ApplicationInsights.Log4NetAppender.NetCoreApp10.Tests, PublicKey=" + AssemblyInfo.PublicKey)]

internal static class AssemblyInfo
{
    // Public key; assemblies are delay signed or OSS signed
    public const string PublicKey = "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9";
}
