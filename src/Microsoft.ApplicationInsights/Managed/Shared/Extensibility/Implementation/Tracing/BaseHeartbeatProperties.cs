namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
#if NETSTANDARD1_3
    using System.Runtime.InteropServices;
#endif

    internal class BaseHeartbeatProperties : IHeartbeatDefaultPayloadProvider
    {
        internal readonly List<string> DefaultFields = new List<string>()
        {
            // "runtimeFramework",
            "baseSdkTargetFramework",
            "osType",
            "processSessionId"
        };

        /// <summary>
        /// A unique identifier that would help to indicate to the analytics when the current process session has
        /// restarted. 
        /// 
        /// <remarks>If a process is unstable and is being restared frequently, tracking this property
        /// in the heartbeat would help to identify this unstability.
        /// </remarks>
        /// </summary>
        private static Guid? uniqueProcessSessionId = null;

        public string Name => "Base";

        public bool IsKeyword(string keyword)
        {
            return this.DefaultFields.Contains(keyword, StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> SetDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatProvider provider)
        {
            bool hasSetValues = false;
            var enabledProperties = this.DefaultFields.Except(disabledFields);

            foreach (string fieldName in enabledProperties)
            {
                // we don't need to report out any failure here, so keep this look within the Sdk Internal Operations as well
                try
                {
                    switch (fieldName)
                    {
                        // case "runtimeFramework":
                        //    provider.AddHeartbeatProperty(fieldName, true, this.GetRuntimeFrameworkVer(), true);
                        //    hasSetValues = true;
                        //    break;
                        case "baseSdkTargetFramework":
                            provider.AddHeartbeatProperty(fieldName, true, this.GetBaseSdkTargetFramework(), true);
                            hasSetValues = true;
                            break;
                        case "osType":
                            provider.AddHeartbeatProperty(fieldName, true, this.GetRuntimeOsType(), true);
                            hasSetValues = true;
                            break;
                        case "processSessionId":
                            provider.AddHeartbeatProperty(fieldName, true, this.GetProcessSessionId(), true);
                            hasSetValues = true;
                            break;
                        default:
                            provider.AddHeartbeatProperty(fieldName, true, "UNDEFINED", true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    CoreEventSource.Log.FailedToObtainDefaultHeartbeatProperty(fieldName, ex.ToString());
                }
            }

            return Task.FromResult(hasSetValues);
        }

        // NOTE: We would like to include the NETSTANDARD/NETCORE/NETFRAMEWORK version in the core heartbeat 
        // but until we understand how to make the mapping from the runtime version to the standard versions
        // this information is not entirely useful. Leaving it commented out here for now until such a mapping
        // can be made in an appropriate manner, or until we can provide such a mapping.
        //
        // <summary>
        // This will return the current running .NET framework version, based on the version of the assembly that owns
        // the 'Object' type. The version number returned can be used to infer other things such as .NET Core / Standard.
        // </summary>
        // <returns>a string representing the version of the current .NET framework</returns>
        // private string GetRuntimeFrameworkVer()
        // {
        // #if NET45 || NET46
        //            Assembly assembly = typeof(Object).GetTypeInfo().Assembly;
        //            AssemblyFileVersionAttribute objectAssemblyFileVer =
        //                        assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
        //                                .Cast<AssemblyFileVersionAttribute>()
        //                                .FirstOrDefault();
        //            return objectAssemblyFileVer != null ? objectAssemblyFileVer.Version : "undefined";
        // #elif NETSTANDARD1_3
        //            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        // #else
        // #error Unrecognized framework
        //            return "unknown";
        // #endif
        // }

        /// <summary>
        /// Returns the current target framework that the assembly was built against.
        /// </summary>
        /// <returns>standard string representing the target framework</returns>
        private string GetBaseSdkTargetFramework()
        {
#if NET45
            return "net45";
#elif NET46
            return "net46";
#elif NETSTANDARD1_3
            return "netstandard1.3";
#else
#error Unrecognized framework
            return "undefined";
#endif
        }

        /// <summary>
        /// Runtime information for the underlying OS, should include Linux information here as well.
        /// Note that in NET45/46 the PlatformId is returned which have slightly different (more specific,
        /// such as Win32NT/Win32S/MacOSX/Unix) values than in NETSTANDARD assemblies where you will get
        /// the OS platform Windows/Linux/OSX.
        /// </summary>
        /// <returns>String representing the OS or 'unknown'.</returns>
        private string GetRuntimeOsType()
        {
            string osValue = "unknown";
#if NET45 || NET46

            osValue = Environment.OSVersion.Platform.ToString();

#elif NETSTANDARD1_3
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osValue = OSPlatform.Linux.ToString();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                osValue = OSPlatform.OSX.ToString();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osValue = OSPlatform.Windows.ToString();
            }
            else
            {
                osValue = RuntimeInformation.OSDescription ?? "unknown";
            }

#else
#error Unrecognized framework
#endif
            return osValue;
        }

        /// <summary>
        /// Return a unique process session identifier that will only be set once in the lifetime of a 
        /// single executable session.
        /// </summary>
        /// <returns>string representation of a unique id</returns>
        private string GetProcessSessionId()
        {
            if (BaseHeartbeatProperties.uniqueProcessSessionId == null)
            {
                BaseHeartbeatProperties.uniqueProcessSessionId = Guid.NewGuid();
            }

            return BaseHeartbeatProperties.uniqueProcessSessionId.ToString();
        }
    }
}
