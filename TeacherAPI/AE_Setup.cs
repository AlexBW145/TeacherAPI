using BaldiPlusArcade;
using HarmonyLib;
using System.Collections.Generic;
using MTM101BaldAPI;
using System.Linq;

namespace TeacherAPI;

public class AETeacherSetting(WeightedTeacher teacher)
{
    public enum TeacherBossAvailability
    {
        VALID,
        BOSSONLY,
        NOTVALID
    }
    public WeightedTeacher teacher = teacher;
    public HashSet<LevelType> excludedLevelTypes = new();
    public TeacherBossAvailability bossValid = TeacherBossAvailability.VALID;
    internal bool CheckValidity(EndlessLevelRepresentation rep) => bossValid switch
    {
        TeacherBossAvailability.BOSSONLY => rep.isBoss,
        TeacherBossAvailability.NOTVALID => !rep.isBoss,
        _ => true
    };
}
[ConditionalPatchMod("mtm101.rulerp.baldiplus.baldiarcade"), HarmonyPatch]
internal static class AE_Setup
{
    public static List<AETeacherSetting>
        teachers = new(),
        assistingTeachers = new();

    [HarmonyPatch(typeof(EndlessLevelRepresentation), "SetupForFloor"), HarmonyPostfix]
    private static void Setup(EndlessLevelRepresentation __instance)
    {
        var Info = TeacherPlugin.Instance.Info;
        __instance.levelObject.SetCustomModValue(Info, "TeacherAPI_PotentialTeachers", teachers.FindAll(x => !x.excludedLevelTypes.Contains(__instance.style.type) && x.CheckValidity(__instance)).Select(x => x.teacher).ToList());
        __instance.levelObject.SetCustomModValue(Info, "TeacherAPI_PotentialAssistants", assistingTeachers.FindAll(x => !x.excludedLevelTypes.Contains(__instance.style.type) && x.CheckValidity(__instance)).Select(x => x.teacher).ToList());
    }
}