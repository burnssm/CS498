using System;

namespace CS498.Lib
{
    public class OpenTimeBlocks
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public string TimeBlock
        {
            get
            {
                return StartDateTime.ToString("t") + " - " + EndDateTime.ToString("t");
            }
        }
    }
}
