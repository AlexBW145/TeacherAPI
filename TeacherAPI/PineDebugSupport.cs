using HarmonyLib;
using MTM101BaldAPI;
using PineDebug;
using System.Collections;

namespace TeacherAPI.PineDebug
{
    internal static class PineDebugSupport
    {
        internal static IEnumerator PineDebugAdds()
        {
            yield return 1;
            yield return "Loading PineDebug Addon: TeacherAPI";
        }
    }

    [ConditionalPatchMod("alexbw145.baldiplus.pinedebug"), HarmonyPatch]
    class PineDebugPatches
    {
        [HarmonyPatch(typeof(Teacher), nameof(Teacher.IsTouchingPlayer))]
        [HarmonyPatch(typeof(Teacher), nameof(Teacher.CaughtPlayer))]
        [HarmonyPrefix]
        static bool NoKills() => !PineDebugManager.BaldiDeathDisabled;
    }
}
