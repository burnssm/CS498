using System;

namespace CS498.Lib
{
    public class TimeBlock
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

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
