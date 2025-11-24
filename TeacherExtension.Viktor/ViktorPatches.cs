using HarmonyLib;
using TeacherAPI;
using UnityEngine;

namespace TeacherExtension.Viktor.Patches;

/*[HarmonyPatch(typeof(BaldiTV))]
class ViktorTV
{
    [HarmonyPatch(typeof(MainGameManager), "AllNotebooks"), HarmonyPrefix]
    static bool ViktorFinalNotebookPrank()
    {
        if (TeacherManager.Instance == null) return true;
        if (TeacherManager.Instance.SpawnedMainTeacher?.GetComponent<Viktor>() != null)
            return TeacherManager.Instance.SpawnedMainTeacher.GetComponent<Viktor>().AllNotebooksPrank;
        return true;
    }
}*/

[HarmonyPatch(typeof(ChalkEraser), "Use")] // I still had the dll...
class ChalkEraserPatch
{
    private static void Postfix(ChalkEraser __instance, bool __result, ref Vector3 ___pos, ref float ___setTime)
    {
        ViktorTilePollutionManager pollutionManager = __instance.ec.GetComponent<ViktorTilePollutionManager>();
        if (pollutionManager != null)
        {
            IntVector2 gridPosition = IntVector2.GetGridPosition(___pos);
            pollutionManager.PolluteCell(__instance.ec.CellFromPosition(gridPosition), ___setTime);
        }
    }
}
[HarmonyPatch(typeof(SteamValveController), nameof(SteamValveController.Set))]
class ValvePatch
{
    private static void Postfix(bool on, SteamValveController __instance, ref DijkstraMap ___spreadMap)
    {
        ViktorTilePollutionManager pollutionManager = __instance.Ec.GetComponent<ViktorTilePollutionManager>();
        if (pollutionManager != null)
        {
            if (on)
            {
                foreach (IntVector2 foundCellPosition in ___spreadMap.FoundCellPositions)
                    pollutionManager.PolluteCell(__instance.Ec.CellFromPosition(foundCellPosition), float.PositiveInfinity);
            }
            else
            {
                foreach (IntVector2 foundCellPosition in ___spreadMap.FoundCellPositions)
                    pollutionManager.UnpolluteCell(__instance.Ec.CellFromPosition(foundCellPosition));
            }
        }
    }
}
[HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.CollectNotebooks))]
class PranksOfAllTime
{
    static void Prefix(int count)
    {
        if (TeacherManager.Instance?.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is Viktor)
        {
            var statebase = TeacherManager.Instance.SpawnedMainTeacher.behaviorStateMachine.CurrentState as Viktor_StateBase;
            statebase.ThePrank(count);
        }
    }
}
