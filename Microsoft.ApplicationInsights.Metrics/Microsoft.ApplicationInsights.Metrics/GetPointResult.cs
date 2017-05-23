using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TPoint"></typeparam>
    public class GetPointResult<TPoint>
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsPointAvailable { get; }

        /// <summary>
        /// 
        /// </summary>
        public TPoint Point { get; }

        internal GetPointResult(bool isPointAvailable, TPoint point)
        {
            this.IsPointAvailable = isPointAvailable;
            this.Point = point;
        }
    }
}
