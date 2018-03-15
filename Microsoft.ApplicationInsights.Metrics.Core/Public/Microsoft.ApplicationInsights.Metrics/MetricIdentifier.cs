using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    public sealed class MetricIdentifier : IEquatable<MetricIdentifier>
    {
        private static readonly char[] InvalidMetricChars = new char[] { '\0', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>', '=', ',',
                                                                         '`',  '~', '!',  '@', '#', '$', '%', '^', '&', '*', '+', '?' };

        
        private static string s_defaultMetricNamespace = "Custom Metrics";

        /// <summary>
        /// This is what metric namespace will be set to if it is not specified.
        /// </summary>
        public static string DefaultMetricNamespace
        {
            get
            {
                return s_defaultMetricNamespace;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                value = value.Trim();
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException($"{nameof(value)} may not be empty.");
                }

                int pos = value.IndexOfAny(InvalidMetricChars);
                if (pos >= 0)
                {
                    throw new ArgumentException($"{nameof(value)} (\"{value}\") contains a disallowed character at position {pos}.");
                }

                s_defaultMetricNamespace = value;
            }
        }

        // These objectes may be created frequently.
        // We want to avoid the allocation of an erray every tim ewe use an ID, so we unwind all loops and list all 10 names explicitly.
        // There is no plan to support more dimension names any time soon.
        private readonly string _dimension1Name;
        private readonly string _dimension2Name;
        private readonly string _dimension3Name;
        private readonly string _dimension4Name;
        private readonly string _dimension5Name;
        private readonly string _dimension6Name;
        private readonly string _dimension7Name;
        private readonly string _dimension8Name;
        private readonly string _dimension9Name;
        private readonly string _dimension10Name;

        private readonly string _identifierString;
        private readonly int _hashCode;

        /// <summary />
        public MetricIdentifier(string metricId)
            : this(metricNamespace: null,
                   metricId: metricId,
                   dimension1Name: null,
                   dimension2Name: null,
                   dimension3Name: null,
                   dimension4Name: null,
                   dimension5Name: null,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(string metricNamespace, string metricId)
            : this(metricNamespace,
                   metricId,
                   dimension1Name: null,
                   dimension2Name: null,
                   dimension3Name: null,
                   dimension4Name: null,
                   dimension5Name: null,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name: null,
                   dimension3Name: null,
                   dimension4Name: null,
                   dimension5Name: null,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name: null,
                   dimension4Name: null,
                   dimension5Name: null,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name: null,
                   dimension5Name: null,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name,
                   dimension5Name: null,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            string dimension5Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name,
                   dimension5Name,
                   dimension6Name: null,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            string dimension5Name,
                            string dimension6Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name,
                   dimension5Name,
                   dimension6Name,
                   dimension7Name: null,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            string dimension5Name,
                            string dimension6Name,
                            string dimension7Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name,
                   dimension5Name,
                   dimension6Name,
                   dimension7Name,
                   dimension8Name: null,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            string dimension5Name,
                            string dimension6Name,
                            string dimension7Name,
                            string dimension8Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name,
                   dimension5Name,
                   dimension6Name,
                   dimension7Name,
                   dimension8Name,
                   dimension9Name: null,
                   dimension10Name: null)
        {
        }

        /// <summary />
        public MetricIdentifier(
                            string metricNamespace,
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            string dimension5Name,
                            string dimension6Name,
                            string dimension7Name,
                            string dimension8Name,
                            string dimension9Name)
            : this(metricNamespace,
                   metricId,
                   dimension1Name,
                   dimension2Name,
                   dimension3Name,
                   dimension4Name,
                   dimension5Name,
                   dimension6Name,
                   dimension7Name,
                   dimension8Name,
                   dimension9Name,
                   dimension10Name: null)
        {
        }



        /// <summary />
        public MetricIdentifier(
                        string metricNamespace,
                        string metricId,
                        string dimension1Name,
                        string dimension2Name,
                        string dimension3Name,
                        string dimension4Name,
                        string dimension5Name,
                        string dimension6Name,
                        string dimension7Name,
                        string dimension8Name,
                        string dimension9Name,
                        string dimension10Name)
        {
            if (String.IsNullOrWhiteSpace(metricNamespace))
            {
                metricNamespace = DefaultMetricNamespace;
            }
            else
            {
                metricNamespace = metricNamespace.Trim();

                int posMNS = metricNamespace.IndexOfAny(InvalidMetricChars);
                if (posMNS >= 0)
                {
                    throw new ArgumentException($"{nameof(metricNamespace)} (\"{metricNamespace}\") contains a disallowed character at position {posMNS}.");
                }
            }

            Util.ValidateNotNullOrWhitespace(metricId, nameof(metricId));

            int posMID = metricId.IndexOfAny(InvalidMetricChars);
            if (posMID >= 0)
            {
                throw new ArgumentException($"{nameof(metricId)} (\"{metricId}\") contains a disallowed character at position {posMID}.");
            }

            metricId = metricId.Trim();

            int dimCount;
            EnsureDimensionNamesValid(
                                out dimCount,
                                ref dimension1Name,
                                ref dimension2Name,
                                ref dimension3Name,
                                ref dimension4Name,
                                ref dimension5Name,
                                ref dimension6Name,
                                ref dimension7Name,
                                ref dimension8Name,
                                ref dimension9Name,
                                ref dimension10Name);

            MetricNamespace = metricNamespace;
            MetricId = metricId;
            DimensionsCount = dimCount;

            _dimension1Name = dimension1Name;
            _dimension2Name = dimension2Name;
            _dimension3Name = dimension3Name;
            _dimension4Name = dimension4Name;
            _dimension5Name = dimension5Name;
            _dimension6Name = dimension6Name;
            _dimension7Name = dimension7Name;
            _dimension8Name = dimension8Name;
            _dimension9Name = dimension9Name;
            _dimension10Name = dimension10Name;

            _identifierString = GetIdentifierString();
            _hashCode = _identifierString.GetHashCode();
        }

        /// <summary />
        public MetricIdentifier(
                        string metricNamespace,
                        string metricId,
                        string[] dimensionNames)
            : this(
                    metricNamespace,
                    metricId,
                    (dimensionNames?.Length > 0) ? dimensionNames[0] : null,
                    (dimensionNames?.Length > 1) ? dimensionNames[1] : null,
                    (dimensionNames?.Length > 2) ? dimensionNames[2] : null,
                    (dimensionNames?.Length > 3) ? dimensionNames[3] : null,
                    (dimensionNames?.Length > 4) ? dimensionNames[4] : null,
                    (dimensionNames?.Length > 5) ? dimensionNames[5] : null,
                    (dimensionNames?.Length > 6) ? dimensionNames[6] : null,
                    (dimensionNames?.Length > 7) ? dimensionNames[7] : null,
                    (dimensionNames?.Length > 8) ? dimensionNames[8] : null,
                    (dimensionNames?.Length > 9) ? dimensionNames[9] : null)
        {
            Util.ValidateNotNull(dimensionNames, nameof(dimensionNames));
            if (dimensionNames.Length > 10)
            {
                throw new ArgumentException($"May not have more than {10} dimensions,"
                                          + $" but {nameof(dimensionNames)} has {dimensionNames.Length} elemets.");
            }
        }

        /// <summary>
        /// The namespace of this metric.
        /// </summary>
        public string MetricNamespace { get; }

        /// <summary>
        /// The ID (name) of this metric.
        /// </summary>
        public string MetricId { get; }

        /// <summary>
        /// The dimensionality of this metric.
        /// </summary>
        public int DimensionsCount { get; }

        /// <summary>
        /// Gets the name of a dimension identified by the specified 1-based dimension index.
        /// </summary>
        /// <param name="dimensionNumber">1-based dimension number. Currently it can be <c>1</c> or <c>2</c>.</param>
        /// <returns>The name of the specified dimension.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233", Justification = "dimensionNumber is validated.")]
        public string GetDimensionName(int dimensionNumber)
        {
            ValidateDimensionNumberForGetter(dimensionNumber);

            switch(dimensionNumber)
            {
                case 1:  return _dimension1Name;
                case 2:  return _dimension2Name;
                case 3:  return _dimension3Name;
                case 4:  return _dimension4Name;
                case 5:  return _dimension5Name;
                case 6:  return _dimension6Name;
                case 7:  return _dimension7Name;
                case 8:  return _dimension8Name;
                case 9:  return _dimension9Name;
                case 10: return _dimension10Name;
                default: throw new ArgumentOutOfRangeException(nameof(dimensionNumber));
            }
        }

        /// <summary />
        /// <returns />
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary />
        /// <param name="otherObj"></param>
        /// <returns />
        public override bool Equals(object otherObj)
        {
            MetricIdentifier otherMetricIdentifier = otherObj as MetricIdentifier;

            if (otherMetricIdentifier != null)
            {
                return Equals(otherMetricIdentifier);
            }
            else
            {
                return base.Equals(otherObj);
            }
        }

        /// <summary />
        /// <param name="otherMetricIdentifier"></param>
        /// <returns />
        public bool Equals(MetricIdentifier otherMetricIdentifier)
        {
            if (otherMetricIdentifier == null)
            {
                return false;
            }

            return (_hashCode == otherMetricIdentifier._hashCode) && (_identifierString.Equals(otherMetricIdentifier._identifierString));
        }

        internal void ValidateDimensionNumberForGetter(int dimensionNumber)
        {
            if (dimensionNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                                nameof(dimensionNumber),
                                $"{dimensionNumber} is an invalid {nameof(dimensionNumber)}. Note that {nameof(dimensionNumber)} is a 1-based index.");
            }

            if (dimensionNumber > 10)
            {
                throw new ArgumentOutOfRangeException(
                                nameof(dimensionNumber),
                                $"{dimensionNumber} is an invalid {nameof(dimensionNumber)}. Only {nameof(dimensionNumber)} = 1, 2, ..., 10 are supported.");
            }

            if (DimensionsCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(dimensionNumber), "Cannot access dimension becasue this metric has no dimensions.");
            }

            if (dimensionNumber > DimensionsCount)
            {
                throw new ArgumentOutOfRangeException($"Cannot access dimension for {nameof(dimensionNumber)}={dimensionNumber}"
                                                    + $" becasue this metric only has {DimensionsCount} dimensions."
                                                    + " Note that {nameof(dimensionNumber)} is a 1-based index.");
            }
        }

        private string GetIdentifierString()
        {
            StringBuilder idStr = new StringBuilder();

            idStr.Append(MetricNamespace);
            idStr.Append("+");
            idStr.Append(MetricId);

            idStr.Append("[");
            idStr.Append(DimensionsCount);
            idStr.Append("]");

            idStr.Append("(");

            for (int d = 1; d <= DimensionsCount; d++)
            {
                if (d > 1)
                {
                    idStr.Append(", ");
                }

                idStr.Append('"');
                idStr.Append(GetDimensionName(d));
                idStr.Append('"');

            }

            idStr.Append(")");

            return idStr.ToString();
        }


        private static void EnsureDimensionNamesValid(
                                                out int dimensionCount,
                                                ref string dimension1Name,
                                                ref string dimension2Name,
                                                ref string dimension3Name,
                                                ref string dimension4Name,
                                                ref string dimension5Name,
                                                ref string dimension6Name,
                                                ref string dimension7Name,
                                                ref string dimension8Name,
                                                ref string dimension9Name,
                                                ref string dimension10Name)
        {
            dimensionCount = 0;
            EnsureDimensionNameValid(ref dimensionCount, ref dimension10Name, 10);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension9Name, 9);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension8Name, 8);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension7Name, 7);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension6Name, 6);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension5Name, 5);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension4Name, 4);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension3Name, 3);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension2Name, 2);
            EnsureDimensionNameValid(ref dimensionCount, ref dimension1Name, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureDimensionNameValid(ref int dimensionCount, ref string dimensionName, int thisDimensionNumber)
        {
            if (dimensionName == null)
            {
                if (dimensionCount != 0)
                {
                    throw new ArgumentException($"Name for dimension number {thisDimensionNumber} may not be omitted,"
                                               + " or may not be null if higher dimensions are present.");
                }

                return;
            }

            dimensionCount = Math.Max(dimensionCount, thisDimensionNumber);

            dimensionName = dimensionName.Trim();

            if (dimensionName.Length == 0)
            {
                throw new ArgumentException($"Name for dimension number {thisDimensionNumber} may not be empty (or whitespace only)."
                                           + " Dimension names may be 'null' to indicate the absence of a dimension, but if present,"
                                           + " they must contain at least 1 printable character.");
            }

            int pos = dimensionName.IndexOfAny(InvalidMetricChars);
            if (pos >= 0)
            {
                throw new ArgumentException($"Name for dimension number {thisDimensionNumber} (\"{dimensionName}\")"
                                          + $" contains a disallowed character at position {pos}.");
            }
        }
    }
}
