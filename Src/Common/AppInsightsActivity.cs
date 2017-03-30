namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    // this is a temporary solution that mimics System.Diagnostics.Activity and Correlation HTTP protocol:
    // https://github.com/lmolkova/correlation/blob/master/http_protocol_proposal_v1.md
    // it is copied from https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/Activity.cs
    // and intended to be used on .NET 4.0
    internal class AppInsightsActivity
    {
        private const int RequestIdMaxLength = 1024;

        // Used to generate an ID:
        // instance unique prefix
        private static string uniqPrefix;

        // A unique number inside the appdomain, randomized between appdomains. 
        // Int gives enough randomization and keeps hex-encoded s_currentRootId 8 chars long for most applications
        private static long currentRootId = (uint)GetRandomNumber();

        public static string GenerateRequestId(string parentId = null)
        {
            string ret;
            if (parentId != null && parentId.Length != 0)
            {
                // Start from outside the process (e.g. incoming HTTP)
                // sanitize external RequestId as it may not be hierarchical. 
                // we cannot update ParentId, we must let it be logged exactly as it was passed.
                parentId = parentId[0] == '|' ? parentId : '|' + parentId;
                if (parentId[parentId.Length - 1] != '.')
                {
                    parentId += '.';
                }

                ret = AppendSuffix(parentId, Interlocked.Increment(ref currentRootId).ToString("x"), '_');
            }
            else
            {
                // A Root Activity (no parent).  
                ret = GenerateRootId();
            }

            // Useful place to place a conditional breakpoint.  
            return ret;
        }

        public static string GenerateDependencyId(string parentId)
        {
            string ret;
            if (parentId != null && parentId.Length != 0)
            {
                // Start from outside the process (e.g. incoming HTTP)
                // sanitize external RequestId as it may not be hierarchical. 
                // we cannot update ParentId, we must let it be logged exactly as it was passed.
                parentId = parentId[0] == '|' ? parentId : '|' + parentId;
                if (parentId[parentId.Length - 1] != '.')
                {
                    parentId += '.';
                }

                ret = AppendSuffix(parentId, Interlocked.Increment(ref currentRootId).ToString("x"), '_');
            }
            else
            {
                // A Root Activity (no parent).  
                ret = GenerateRootId();
            }

            // Useful place to place a conditional breakpoint.  
            return ret;
        }

        public static string GetRootId(string id)
        {
            // id MAY start with '|' and contain '.'. We return substring between them
            // ParentId MAY NOT have hierarchical structure and we don't know if initially rootId was started with '|',
            // so we must NOT include first '|' to allow mixed hierarchical and non-hierarchical request id scenarios
            int rootEnd = id.IndexOf('.');
            if (rootEnd < 0)
            {
                rootEnd = id.Length;
            }

            int rootStart = id[0] == '|' ? 1 : 0;
            return id.Substring(rootStart, rootEnd - rootStart);
        }

        private static string AppendSuffix(string parentId, string suffix, char delimiter)
        {
            if (parentId.Length + suffix.Length < RequestIdMaxLength)
            {
                return parentId + suffix + delimiter;
            }

            // Id overflow:
            // find position in RequestId to trim
            int trimPosition = RequestIdMaxLength - 9; // overflow suffix + delimiter length is 9
            while (trimPosition > 1)
            {
                if (parentId[trimPosition - 1] == '.' || parentId[trimPosition - 1] == '_')
                {
                    break;
                }

                trimPosition--;
            }

            // ParentId is not valid Request-Id, let's generate proper one.
            if (trimPosition == 0)
            {
                return GenerateRootId();
            }

            // generate overflow suffix
            string overflowSuffix = ((int)GetRandomNumber()).ToString("x8");
            return parentId.Substring(0, trimPosition) + overflowSuffix + '#';
        }

        private static string GenerateRootId()
        {
            if (uniqPrefix == null)
            {
                // Here we make an ID to represent the Process/AppDomain.   Ideally we use process ID but 
                // it is unclear if we have that ID handy.   Currently we use low bits of high freq tick 
                // as a unique random number (which is not bad, but loses randomness for startup scenarios).  
                Interlocked.CompareExchange(ref uniqPrefix, GenerateInstancePrefix(), null);
            }

            return uniqPrefix + "-" + Interlocked.Increment(ref currentRootId).ToString("x") + '.';
        }

        private static string GenerateInstancePrefix()
        {
            int uniqNum = unchecked((int)Stopwatch.GetTimestamp());
            return $"|{Environment.MachineName}-{uniqNum:x}";
        }

        private static ulong GetRandomNumber()
        {
            return BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 8);
        }
    }
}