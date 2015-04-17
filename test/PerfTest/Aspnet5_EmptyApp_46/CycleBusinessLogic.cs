namespace Aspnet5_EmptyApp_46
{
    using System;
    
    public class CycleBusinessLogic
    {
        private readonly int timeToCycleInMs;
        private DateTime logicTime;

        public CycleBusinessLogic(int timeToCycleInMs)
        {
            this.timeToCycleInMs = timeToCycleInMs;
        }

        public DateTime LogicTime
        {
            get
            {
                return this.logicTime;
            }
        }

        public void Execute()
        {
            DateTime finishTime = DateTime.Now.AddMilliseconds(this.timeToCycleInMs);
            this.logicTime = DateTime.Now;

            while (logicTime < finishTime)
            {
                this.logicTime = DateTime.Now;
            }
        }
    }
}