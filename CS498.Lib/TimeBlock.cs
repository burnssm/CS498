using Google.Apis.Calendar.v3.Data;
using System;

namespace CS498.Lib
{
    public class TimeBlock
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public TimeBlock() { }

        public TimeBlock(DateTime now)
        {
            Start = now;
            End = now;
        }
        public TimeBlock(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
        public TimeBlock(EventDateTime start, EventDateTime end)
        {
            Start = start.DateTime.GetValueOrDefault();
            End = end.DateTime.GetValueOrDefault();
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
