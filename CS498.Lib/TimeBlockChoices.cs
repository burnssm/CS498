using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS498.Lib
{
    public enum TimeBlockChoices
    {
        [Description("Next 24 Hours")]
        Today = 1,
        [Description("Next 48 Hours")]
        Tomorrow = 2,
        [Description("Next Full Week")]
        FullWeek = 7,
        [Description("Next Two Weeks")]
        TwoWeeks = 14
    }
}
