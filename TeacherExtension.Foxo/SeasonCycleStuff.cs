using BaldiPlus_Seasons;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeacherExtension.Foxo
{
    public static class SeasonCycleStuff // Too short of a code...
    {
        public static bool CheckIfNight() => (BasePlugin.eastern && CycleManager.Instance.time >= 6 && CycleManager.Instance.time <= 17)
            || (!BasePlugin.eastern && (CycleManager.Instance.time >= 18 || CycleManager.Instance.time <= 5));
    }
}
