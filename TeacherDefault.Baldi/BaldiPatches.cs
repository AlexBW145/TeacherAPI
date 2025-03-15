using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TeacherAPI;

namespace TeacherExtension.Baldimore
{
    [HarmonyPatch(typeof(Baldi), nameof(Baldi.Praise))]
    class NoPraise // That's it??
    {
        static bool Prefix(Baldi __instance) => !(__instance is TeacherBaldi && __instance.behaviorStateMachine.currentState is TeacherBaldi_Happy);
    }
}
