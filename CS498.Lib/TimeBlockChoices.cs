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
        [Description("Today")]
        Today = 1,
        [Description("Tomorrow")]
        Tomorrow = 2,
        [Description("Full Week")]
        FullWeek = 7,
        [Description("Two Weeks")]
        TwoWeeks = 14
    }
}
