namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Represents a zero- or multi-dimensional metric.<br />
    /// Contains convenience methods to track, aggregate and send values.<br />
    /// A <c>Metric</c> instance groups one or more <c>MetricSeries</c> that actually track and aggregate values along with
    /// naming and configuration attributes that identify the metric and define how it will be aggregated. 
    /// </summary>
    public sealed class Metric
    {
        private readonly TelemetryClient client;
        private readonly string metricName;
        private readonly string metricNamespace;
        private readonly string[] dimensionNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metric"/> class.
        /// </summary>
        /// <param name="client">The telemetry client.</param>
        /// <param name="metricName">The metric name.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="dimensionNames">The dimension names.</param>
        internal Metric(TelemetryClient client, string metricName, string metricNamespace, string[] dimensionNames)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.metricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
            this.metricNamespace = metricNamespace;
            this.dimensionNames = dimensionNames ?? Array.Empty<string>();
        }

        /// <summary>
        /// Tracks the specified value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.<br />
        /// This method uses the zero-dimensional <c>MetricSeries</c> associated with this metric.
        /// Use <c>TrackValue(..)</c> to track values into <c>MetricSeries</c> associated with specific dimension-values in multi-dimensional metrics.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        public void TrackValue(double metricValue)
        {
            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            histogram.Record(metricValue);
        }

        /// <summary>
        /// Tracks the specified value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.<br />
        /// This method uses the zero-dimensional <c>MetricSeries</c> associated with this metric.
        /// Use <c>TrackValue(..)</c> to track values into <c>MetricSeries</c> associated with specific dimension-values in multi-dimensional metrics.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        public void TrackValue(object metricValue)
        {
            if (metricValue == null)
            {
                return;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                this.TrackValue(value);
            }
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 1-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(double metricValue, string dimension1Value)
        {
            if (this.dimensionNames.Length != 1)
            {
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 1-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(object metricValue, string dimension1Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 2-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(double metricValue, string dimension1Value, string dimension2Value)
        {
            if (this.dimensionNames.Length != 2)
            {
                // throw new ArgumentException("This metric expects 2 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 2-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(object metricValue, string dimension1Value, string dimension2Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 3-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(double metricValue, string dimension1Value, string dimension2Value, string dimension3Value)
        {
            if (this.dimensionNames.Length != 3)
            {
                // throw new ArgumentException("This metric expects 3 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 3-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(object metricValue, string dimension1Value, string dimension2Value, string dimension3Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 4-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(double metricValue, string dimension1Value, string dimension2Value, string dimension3Value, string dimension4Value)
        {
            if (this.dimensionNames.Length != 4)
            {
                // throw new ArgumentException("This metric expects 4 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 4-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(object metricValue, string dimension1Value, string dimension2Value, string dimension3Value, string dimension4Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 5-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                double metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value)
        {
            if (this.dimensionNames.Length != 5)
            {
                // throw new ArgumentException("This metric expects 5 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
                { this.dimensionNames[4], dimension5Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 5-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                object metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value, dimension5Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 6-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                double metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value)
        {
            if (this.dimensionNames.Length != 6)
            {
                // throw new ArgumentException("This metric expects 6 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
                { this.dimensionNames[4], dimension5Value },
                { this.dimensionNames[5], dimension6Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 6-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                object metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value, dimension5Value, dimension6Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 7-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                double metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value)
        {
            if (this.dimensionNames.Length != 7)
            {
                // throw new ArgumentException("This metric expects 7 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
                { this.dimensionNames[4], dimension5Value },
                { this.dimensionNames[5], dimension6Value },
                { this.dimensionNames[6], dimension7Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 7-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                object metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value, dimension5Value, dimension6Value, dimension7Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 8-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <param name="dimension8Value">The value of the 8th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                double metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value,
                                string dimension8Value)
        {
            if (this.dimensionNames.Length != 8)
            {
                // throw new ArgumentException("This metric expects 8 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
                { this.dimensionNames[4], dimension5Value },
                { this.dimensionNames[5], dimension6Value },
                { this.dimensionNames[6], dimension7Value },
                { this.dimensionNames[7], dimension8Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 8-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <param name="dimension8Value">The value of the 8th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                object metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value,
                                string dimension8Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value, dimension5Value, dimension6Value, dimension7Value, dimension8Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 9-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <param name="dimension8Value">The value of the 8th dimension.</param>
        /// <param name="dimension9Value">The value of the 9th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                double metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value,
                                string dimension8Value,
                                string dimension9Value)
        {
            if (this.dimensionNames.Length != 9)
            {
                // throw new ArgumentException("This metric expects 9 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
                { this.dimensionNames[4], dimension5Value },
                { this.dimensionNames[5], dimension6Value },
                { this.dimensionNames[6], dimension7Value },
                { this.dimensionNames[7], dimension8Value },
                { this.dimensionNames[8], dimension9Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 9-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <param name="dimension8Value">The value of the 8th dimension.</param>
        /// <param name="dimension9Value">The value of the 9th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                object metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value,
                                string dimension8Value,
                                string dimension9Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value, dimension5Value, dimension6Value, dimension7Value, dimension8Value, dimension9Value);
            }

            return false;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 10-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <param name="dimension8Value">The value of the 8th dimension.</param>
        /// <param name="dimension9Value">The value of the 9th dimension.</param>
        /// <param name="dimension10Value">The value of the 10th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                double metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value,
                                string dimension8Value,
                                string dimension9Value,
                                string dimension10Value)
        {
            if (this.dimensionNames.Length != 10)
            {
                // throw new ArgumentException("This metric expects 10 dimension values.");
                // TODO: Log exception
                return false;
            }

            var histogram = this.client.TelemetryConfiguration.MetricsManager.GetOrCreateHistogram(
                this.metricName,
                this.metricNamespace);

            var tags = new TagList
            {
                { this.dimensionNames[0], dimension1Value },
                { this.dimensionNames[1], dimension2Value },
                { this.dimensionNames[2], dimension3Value },
                { this.dimensionNames[3], dimension4Value },
                { this.dimensionNames[4], dimension5Value },
                { this.dimensionNames[5], dimension6Value },
                { this.dimensionNames[6], dimension7Value },
                { this.dimensionNames[7], dimension8Value },
                { this.dimensionNames[8], dimension9Value },
                { this.dimensionNames[9], dimension10Value },
            };

            histogram.Record(metricValue, tags);
            return true;
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 10-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="dimension3Value">The value of the 3rd dimension.</param>
        /// <param name="dimension4Value">The value of the 4th dimension.</param>
        /// <param name="dimension5Value">The value of the 5th dimension.</param>
        /// <param name="dimension6Value">The value of the 6th dimension.</param>
        /// <param name="dimension7Value">The value of the 7th dimension.</param>
        /// <param name="dimension8Value">The value of the 8th dimension.</param>
        /// <param name="dimension9Value">The value of the 9th dimension.</param>
        /// <param name="dimension10Value">The value of the 10th dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached,
        /// or if the number of specified dimension values does not match the dimensionality of this <c>Metric</c>.</returns>
        public bool TrackValue(
                                object metricValue,
                                string dimension1Value,
                                string dimension2Value,
                                string dimension3Value,
                                string dimension4Value,
                                string dimension5Value,
                                string dimension6Value,
                                string dimension7Value,
                                string dimension8Value,
                                string dimension9Value,
                                string dimension10Value)
        {
            if (metricValue == null)
            {
                return false;
            }

            if (double.TryParse(metricValue.ToString(), out double value))
            {
                return this.TrackValue(value, dimension1Value, dimension2Value, dimension3Value, dimension4Value, dimension5Value, dimension6Value, dimension7Value, dimension8Value, dimension9Value, dimension10Value);
            }

            return false;
        }
    }
}