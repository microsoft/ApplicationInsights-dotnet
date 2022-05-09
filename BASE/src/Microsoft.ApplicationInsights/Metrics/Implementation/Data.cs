namespace Microsoft.ApplicationInsights.Metrics
{
    using System;

    internal class Data
    {
        public int Count = 0;
        public double Min = Double.MaxValue;
        public double Max = Double.MinValue;
        public double Sum = 0.0;
        public double SumOfSquares = 0.0;

        private readonly bool restrictToUInt32Values;

        public Data(bool restrictToUInt32Values)
        {
            this.restrictToUInt32Values = restrictToUInt32Values;
        }

        public void UpdateAggregate(double metricValue)
        {
            if (Double.IsNaN(metricValue))
            {
                return;
            }

            this.Count++;
            this.Max = (metricValue > this.Max) ? metricValue : this.Max;
            this.Min = (metricValue < this.Min) ? metricValue : this.Min;
            this.Sum += metricValue;
            this.SumOfSquares += metricValue * metricValue;

            if (this.restrictToUInt32Values)
            {
                this.Sum = Math.Round(this.Sum);
                this.SumOfSquares = Math.Round(this.SumOfSquares);
            }
        }

        public void UpdateAggregate(Data data)
        {
            if (data.Count == 0)
            {
                return;
            }

            this.Count += data.Count;
            this.Max = (data.Max > this.Max) ? data.Max : this.Max;
            this.Min = (data.Min < this.Min) ? data.Min : this.Min;
            this.Sum += data.Sum;
            this.SumOfSquares += data.SumOfSquares;

            if (this.restrictToUInt32Values)
            {
                this.Sum = Math.Round(this.Sum);
                this.SumOfSquares = Math.Round(this.SumOfSquares);
            }
        }

        public void ResetAggregate()
        {
            this.Count = 0;
            this.Min = Double.MaxValue;
            this.Max = Double.MinValue;
            this.Sum = 0.0;
            this.SumOfSquares = 0.0;
        }
    }
}
