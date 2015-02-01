using System;

namespace CS498.Lib
{
    public class TimeBlock
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public TimeBlock(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
        public override string ToString()
        {
            return Start.ToString("t") + " - " + End.ToString("t");
        }

        public string Display
        {
            get { return ToString(); }
        }
    }
}
