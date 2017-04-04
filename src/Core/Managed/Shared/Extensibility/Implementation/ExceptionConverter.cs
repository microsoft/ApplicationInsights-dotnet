namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal static class ExceptionConverter
    {
        public const int MaxParsedStackLength = 32768;

        /// <summary>
        /// Converts a System.Exception to a Microsoft.ApplicationInsights.Extensibility.Implementation.TelemetryTypes.ExceptionDetails.
        /// </summary>
        internal static External.ExceptionDetails ConvertToExceptionDetails(
            Exception exception,
            External.ExceptionDetails parentExceptionDetails)
        {
            External.ExceptionDetails exceptionDetails = External.ExceptionDetails.CreateWithoutStackInfo(
                                                                                                                exception,
                                                                                                                parentExceptionDetails);
#if !NETSTANDARD1_3
            var stack = new StackTrace(exception, true);

            var frames = stack.GetFrames();
            Tuple<List<External.StackFrame>, bool> sanitizedTuple = SanitizeStackFrame(
                                                                                        frames,
                                                                                        GetStackFrame,
                                                                                        GetStackFrameLength);
            exceptionDetails.parsedStack = sanitizedTuple.Item1;
            exceptionDetails.hasFullStack = sanitizedTuple.Item2;
#else
            if (exception.StackTrace != null)
            {
                string[] lines = exception.StackTrace.Split(new string[] { "\n" }, StringSplitOptions.None);

                // Adding 1 for length in lengthGetter for newline character
                Tuple<List<string>, bool> sanitizedTuple = SanitizeStackFrame(
                                                                            lines,
                                                                            (input, id) => input,
                                                                            (input) => input == null ? 0 : input.Length + 1);
                List<string> sanitizedStackLines = sanitizedTuple.Item1;
                exceptionDetails.hasFullStack = sanitizedTuple.Item2;
                exceptionDetails.stack = string.Join("\n", sanitizedStackLines.ToArray());
            }
            else
            {
                exceptionDetails.hasFullStack = true;
                exceptionDetails.stack = string.Empty;
            }
#endif
            return exceptionDetails;
        }

        /// <summary>
        /// Sanitizing stack to 32k while selecting the initial and end stack trace.
        /// </summary>
        private static Tuple<List<TOutput>, bool> SanitizeStackFrame<TInput, TOutput>(
            IList<TInput> inputList,
            Func<TInput, int, TOutput> converter,
            Func<TOutput, int> lengthGetter)
        {
            List<TOutput> orderedStackTrace = new List<TOutput>();
            bool hasFullStack = true;
            if (inputList != null && inputList.Count > 0)
            {
                int currentParsedStackLength = 0;
                for (int level = 0; level < inputList.Count; level++)
                {
                    // Skip middle part of the stack
                    int current = (level % 2 == 0) ? (inputList.Count - 1 - (level / 2)) : (level / 2);

                    TOutput convertedStackFrame = converter(inputList[current], current);
                    currentParsedStackLength += lengthGetter(convertedStackFrame);

                    if (currentParsedStackLength > ExceptionConverter.MaxParsedStackLength)
                    {
                        hasFullStack = false;
                        break;
                    }

                    orderedStackTrace.Insert(orderedStackTrace.Count / 2, convertedStackFrame);
                }
            }

            return new Tuple<List<TOutput>, bool>(orderedStackTrace, hasFullStack);
        }

#if !NETSTANDARD1_3
       /// <summary>
        /// Converts a System.Diagnostics.StackFrame to a Microsoft.ApplicationInsights.Extensibility.Implementation.TelemetryTypes.StackFrame.
        /// </summary>
        internal static External.StackFrame GetStackFrame(StackFrame stackFrame, int frameId)
        {
            var convertedStackFrame = new External.StackFrame()
            {
                level = frameId
            };

            var methodInfo = stackFrame.GetMethod();
            string fullName;
            if (methodInfo.DeclaringType != null)
            {
                fullName = methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
            }
            else
            {
                fullName = methodInfo.Name;
            }

            convertedStackFrame.method = fullName;
            convertedStackFrame.assembly = methodInfo.Module.Assembly.FullName;
            convertedStackFrame.fileName = stackFrame.GetFileName();

            // 0 means it is unavailable
            int line = stackFrame.GetFileLineNumber();
            if (line != 0)
            {
                convertedStackFrame.line = line;
            }

            return convertedStackFrame;
        }
        
        /// <summary>
        /// Gets the stack frame length for only the strings in the stack frame.
        /// </summary>
        internal static int GetStackFrameLength(External.StackFrame stackFrame)
        {
            var stackFrameLength = (stackFrame.method == null ? 0 : stackFrame.method.Length)
                                   + (stackFrame.assembly == null ? 0 : stackFrame.assembly.Length)
                                   + (stackFrame.fileName == null ? 0 : stackFrame.fileName.Length);
            return stackFrameLength;
        }
#endif
    }
}