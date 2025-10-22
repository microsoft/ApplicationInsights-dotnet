using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Microsoft.ApplicationInsights.AspNetCore.Tests, PublicKey=" + AssemblyInfo.PublicKey)]
[assembly: ComVisible(false)]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "SDK intentionally catch ALL exceptions and never re-throws.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File must have header", Justification = "Unnecessary.")]
