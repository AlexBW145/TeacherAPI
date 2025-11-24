using HarmonyLib;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(HappyBaldi), "OnTriggerExit")]
    internal class OnTriggerExitPatch
    {
        internal static bool Prefix()
        {
            if (TeacherManager.Instance == null) return true;
            if (TeacherManager.Instance.spawnedTeachers.Count > 0)
            {
                foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
                {
                    teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.PlayerExitedSpawn());
                }
                // Don't want baldi to mess up with the spawn
                return false;
            }

            return !TeacherManager.Instance.SpoopModeActivated;
        }
    }

    [HarmonyPatch(typeof(RulerEvent))]
    internal class RulerEventPatches
    {
        [HarmonyPatch(nameof(RulerEvent.Begin)), HarmonyPrefix]
        public static bool BreakRuler()
        {
            if (TeacherManager.Instance?.MainTeacherPrefab == null || TeacherManager.DefaultBaldiEnabled) return true;
            TeacherManager.Instance?.DoIfMainTeacher(t => t.BreakRuler());
            return false;
        }
        [HarmonyPatch(nameof(RulerEvent.End)), HarmonyPrefix]
        public static bool RestoreRuler()
        {
            if (TeacherManager.Instance?.MainTeacherPrefab == null || TeacherManager.DefaultBaldiEnabled) return true;
            TeacherManager.Instance?.DoIfMainTeacher(t => t.RestoreRuler());
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.PleaseBaldi))]
    internal class OnGoodMathMachineAnswer
    {
        internal static void Prefix()
        {
            if (TeacherManager.Instance == null) return;
            foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
            {
                teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.GoodMathMachineAnswer());
            }
        }
    }
}
