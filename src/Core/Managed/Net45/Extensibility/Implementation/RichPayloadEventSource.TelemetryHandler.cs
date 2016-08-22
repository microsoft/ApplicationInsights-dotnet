namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Define the telemetry handlers.
    /// Create anonymous type for each telemetry data type.
    /// </summary>
    internal sealed partial class RichPayloadEventSource
    {
        private readonly string dummyPartAiKeyValue = string.Empty;
        private readonly IDictionary<string, string> dummyPartATagsValue = new Dictionary<string, string>();

        private void InitTelemetryHandlers(Type eventSourceType)
        {
            // EventSource.Write<T> (String, EventSourceOptions, T)
            var writeGenericMethod = eventSourceType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == "Write" && m.IsGenericMethod == true)
                .Select(m => new { Method = m, Parameters = m.GetParameters() })
                .Where(m => m.Parameters.Length == 3
                            && m.Parameters[0].ParameterType.FullName == "System.String"
                            && m.Parameters[1].ParameterType.FullName == "System.Diagnostics.Tracing.EventSourceOptions"
                            && m.Parameters[2].ParameterType.FullName == null && m.Parameters[2].ParameterType.IsByRef == false)
                .Select(m => m.Method)
                .SingleOrDefault();

            if (writeGenericMethod != null)
            {
                var eventSourceOptionsType = eventSourceType.Assembly.GetType("System.Diagnostics.Tracing.EventSourceOptions");
                var eventSourceOptionsKeywordsProperty = eventSourceOptionsType.GetProperty("Keywords", BindingFlags.Public | BindingFlags.Instance);

                // Request
                this.telemetryHandlers.Add(typeof(RequestTelemetry), this.CreateHandlerForRequestTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // Trace
                this.telemetryHandlers.Add(typeof(TraceTelemetry), this.CreateHandlerForTraceTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // Event
                this.telemetryHandlers.Add(typeof(EventTelemetry), this.CreateHandlerForEventTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // Dependency
                this.telemetryHandlers.Add(typeof(DependencyTelemetry), this.CreateHandlerForDependencyTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // Metric
                this.telemetryHandlers.Add(typeof(MetricTelemetry), this.CreateHandlerForMetricTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // Exception
                this.telemetryHandlers.Add(typeof(ExceptionTelemetry), this.CreateHandlerForExceptionTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // PerformanceCounter
                this.telemetryHandlers.Add(typeof(PerformanceCounterTelemetry), this.CreateHandlerForPerformanceCounterTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // PageView
                this.telemetryHandlers.Add(typeof(PageViewTelemetry), this.CreateHandlerForPageViewTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));

                // SessionState
                this.telemetryHandlers.Add(typeof(SessionStateTelemetry), this.CreateHandlerForSessionStateTelemetry(writeGenericMethod, eventSourceOptionsType, eventSourceOptionsKeywordsProperty));
            }
            else
            {
                CoreEventSource.Log.LogVerbose("Unable to get method: EventSource.Write<T>(String, EventSourceOptions, T)");
            }
        }

        private Action<ITelemetry> CreateHandlerForRequestTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.Requests;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyRequestData = new RequestData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_RequestData = new
                {
                    // The properties and layout should be the same as RequestData_types.cs
                    dummyRequestData.ver,
                    dummyRequestData.id,
                    dummyRequestData.source,
                    dummyRequestData.name,
                    dummyRequestData.startTime,
                    dummyRequestData.duration,
                    dummyRequestData.responseCode,
                    dummyRequestData.success,
                    dummyRequestData.httpMethod,
                    dummyRequestData.url,
                    dummyRequestData.properties,
                    dummyRequestData.measurements
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as RequestTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_RequestData = new
                        {
                            data.ver,
                            data.id,
                            data.source,
                            data.name,
                            data.startTime,
                            data.duration,
                            data.responseCode,
                            data.success,
                            data.httpMethod,
                            data.url,
                            data.properties,
                            data.measurements
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { RequestTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForTraceTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.Traces;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyMessageData = new MessageData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_MessageData = new
                {
                    // The properties and layout should be the same as MessageData_types.cs
                    dummyMessageData.ver,
                    dummyMessageData.message,
                    dummyMessageData.severityLevel,
                    dummyMessageData.properties
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as TraceTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_MessageData = new
                        {
                            data.ver,
                            data.message,
                            data.severityLevel,
                            data.properties
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { TraceTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForEventTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.Events;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyEventData = new EventData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_EventData = new
                {
                    // The properties and layout should be the same as EventData_types.cs
                    dummyEventData.ver,
                    dummyEventData.name,
                    dummyEventData.properties,
                    dummyEventData.measurements
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as EventTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_EventData = new
                        {
                            data.ver,
                            data.name,
                            data.properties,
                            data.measurements
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { EventTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForDependencyTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.Dependencies;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyDependencyData = new RemoteDependencyData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_RemoteDependencyData = new
                {
                    // The properties and layout should be the same as RemoteDependencyData_types.cs
                    dummyDependencyData.ver,
                    dummyDependencyData.name,
                    dummyDependencyData.id,
                    dummyDependencyData.resultCode,
                    dummyDependencyData.kind,
                    dummyDependencyData.value,
                    dummyDependencyData.duration,
                    dummyDependencyData.dependencyKind,
                    dummyDependencyData.success,
                    dummyDependencyData.async,
                    dummyDependencyData.dependencySource,
                    dummyDependencyData.commandName,
                    dummyDependencyData.data,
                    dummyDependencyData.dependencyTypeName,
                    dummyDependencyData.target,
                    dummyDependencyData.properties,
                    dummyDependencyData.measurements
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as DependencyTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_RemoteDependencyData = new
                        {
                            data.ver,
                            data.name,
                            data.id,
                            data.resultCode,
                            data.kind,
                            data.value,
                            data.duration,
                            data.dependencyKind,
                            data.success,
                            data.async,
                            data.dependencySource,
                            data.commandName,
                            data.data,
                            data.dependencyTypeName,
                            data.target,
                            data.properties,
                            data.measurements
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { DependencyTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForMetricTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.Metrics;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyMetricData = new MetricData();
            var dummyDataPoint = new DataPoint();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_MetricData = new
                {
                    // The properties and layout should be the same as MetricData_types.cs
                    dummyMetricData.ver,
                    metrics = new[]
                    {
                        new
                        {
                            // The properties and layout should be the same as DataPoint_types.cs
                            dummyDataPoint.name,
                            dummyDataPoint.kind,
                            dummyDataPoint.value,
                            dummyDataPoint.count,
                            dummyDataPoint.min,
                            dummyDataPoint.max,
                            dummyDataPoint.stdDev
                        }
                    }.AsEnumerable(),
                    dummyMetricData.properties
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as MetricTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_MetricData = new
                        {
                            data.ver,
                            metrics = data.metrics.Select(i => new
                            {
                                i.name,
                                i.kind,
                                i.value,
                                i.count,
                                i.min,
                                i.max,
                                i.stdDev
                            }),
                            data.properties
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { MetricTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForExceptionTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.Exceptions;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyExceptionData = new ExceptionData();
            var dummyExceptionDetails = new ExceptionDetails();
            var dummyStackFrame = new StackFrame();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_ExceptionData = new
                {
                    // The properties and layout should be the same as ExceptionData_types.cs
                    dummyExceptionData.ver,
                    dummyExceptionData.handledAt,
                    exceptions = new[]
                    {
                        new
                        {
                            // The properties and layout should be the same as ExceptionDetails_types.cs
                            dummyExceptionDetails.id,
                            dummyExceptionDetails.outerId,
                            dummyExceptionDetails.typeName,
                            dummyExceptionDetails.message,
                            dummyExceptionDetails.hasFullStack,
                            dummyExceptionDetails.stack,
                            parsedStack = new[]
                            {
                                new
                                {
                                    // The properties and layout should be the same as StackFrame_types.cs
                                    dummyStackFrame.level,
                                    dummyStackFrame.method,
                                    dummyStackFrame.assembly,
                                    dummyStackFrame.fileName,
                                    dummyStackFrame.line
                                }
                            }.AsEnumerable()
                        }
                    }.AsEnumerable(),
                    dummyExceptionData.severityLevel,
                    dummyExceptionData.problemId,
                    dummyExceptionData.properties,
                    dummyExceptionData.measurements,
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as ExceptionTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_ExceptionData = new
                        {
                            data.ver,
                            data.handledAt,
                            exceptions = data.exceptions.Select(i => new
                            {
                                i.id,
                                i.outerId,
                                i.typeName,
                                i.message,
                                i.hasFullStack,
                                i.stack,
                                parsedStack = i.parsedStack.Select(j => new
                                {
                                    j.level,
                                    j.method,
                                    j.assembly,
                                    j.fileName,
                                    j.line
                                }),
                            }),
                            data.severityLevel,
                            data.problemId,
                            data.properties,
                            data.measurements
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { ExceptionTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForPerformanceCounterTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.PerformanceCounters;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyPerfData = new PerformanceCounterData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_PerformanceCounterData = new
                {
                    // The properties and layout should be the same as PerformanceCounterData_types.cs
                    dummyPerfData.ver,
                    dummyPerfData.categoryName,
                    dummyPerfData.counterName,
                    dummyPerfData.instanceName,
                    dummyPerfData.kind,
                    dummyPerfData.count,
                    dummyPerfData.min,
                    dummyPerfData.max,
                    dummyPerfData.stdDev,
                    dummyPerfData.value,
                    dummyPerfData.properties
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as PerformanceCounterTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_PerformanceCounterData = new
                        {
                            data.ver,
                            data.categoryName,
                            data.counterName,
                            data.instanceName,
                            data.kind,
                            data.count,
                            data.min,
                            data.max,
                            data.stdDev,
                            data.value,
                            data.properties
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { PerformanceCounterTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForPageViewTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.PageViews;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummyPageViewData = new PageViewData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_PageViewPerfData = new
                {
                    // The properties and layout should be the same as PageViewData_types.cs (EventData_types.cs)
                    dummyPageViewData.url,
                    dummyPageViewData.duration,
                    dummyPageViewData.ver,
                    dummyPageViewData.name,
                    dummyPageViewData.properties,
                    dummyPageViewData.measurements,
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as PageViewTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_PageViewPerfData = new
                        {
                            data.url,
                            data.duration,
                            data.ver,
                            data.name,
                            data.properties,
                            data.measurements,
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { PageViewTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }

        private Action<ITelemetry> CreateHandlerForSessionStateTelemetry(MethodInfo writeGenericMethod, Type eventSourceOptionsType, PropertyInfo eventSourceOptionsKeywordsProperty)
        {
            var eventSourceOptions = Activator.CreateInstance(eventSourceOptionsType);
            var keywords = Keywords.SessionState;
            eventSourceOptionsKeywordsProperty.SetValue(eventSourceOptions, keywords);
            var dummySessionStateData = new SessionStateData();
            var writeMethod = writeGenericMethod.MakeGenericMethod(new
            {
                PartA_iKey = this.dummyPartAiKeyValue,
                PartA_Tags = this.dummyPartATagsValue,
                PartB_SessionStateData = new
                {
                    // The properties and layout should be the same as SessionStateData_types.cs
                    dummySessionStateData.ver,
                    dummySessionStateData.state
                }
            }.GetType());

            return (item) =>
            {
                if (this.EventSourceInternal.IsEnabled(EventLevel.Verbose, keywords))
                {
                    var telemetryItem = item as SessionStateTelemetry;
                    var data = telemetryItem.Data;
                    var extendedData = new
                    {
                        // The properties and layout should be the same as the anonymous type in the above MakeGenericMethod
                        PartA_iKey = telemetryItem.Context.InstrumentationKey,
                        PartA_Tags = telemetryItem.Context.Tags,
                        PartB_SessionStateData = new
                        {
                            data.ver,
                            data.state
                        }
                    };

                    writeMethod.Invoke(this.EventSourceInternal, new object[] { SessionStateTelemetry.TelemetryName, eventSourceOptions, extendedData });
                }
            };
        }
    }
}
