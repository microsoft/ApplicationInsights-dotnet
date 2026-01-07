using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Microsoft.ApplicationInsights.WorkerService.Tests, PublicKey=" + AssemblyInfo.PublicKey)]
[assembly: InternalsVisibleTo("WorkerIntegrationTests.Tests, PublicKey=" + AssemblyInfo.PublicKey)]
[assembly: ComVisible(false)]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File must have header", Justification = "Unnecessary.")]