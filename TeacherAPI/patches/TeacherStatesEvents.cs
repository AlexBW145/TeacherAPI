using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;

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
                // Don't want baldi to mess up with the spawn
                return false;
            }

            return !TeacherManager.Instance.SpoopModeActivated;
        }
    }

    [HarmonyPatch(typeof(ElevatorManager), nameof(ElevatorManager.PlayerExitedSpawn))] // Good change??
    class ElevatorExitPatch
    {
        static void Prefix(bool ___playerHasExitedSpawn)
        {
            if (!___playerHasExitedSpawn)
            {
                if (TeacherManager.Instance == null) return;
                foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
                    teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.PlayerExitedSpawn()); // It was misleading for some reason...
            }
        }
    }

    [HarmonyPatch(typeof(RulerEvent))]
    internal class RulerEventPatches
    {
        private static readonly MethodInfo _Timer = AccessTools.Method(typeof(RandomEvent), "Timer"),
            _Begin = AccessTools.Method(typeof(RandomEvent), nameof(RandomEvent.Begin)),
            _End = AccessTools.Method(typeof(RandomEvent), nameof(RandomEvent.End));
        [HarmonyPatch(nameof(RulerEvent.Begin)), HarmonyPrefix]
        public static bool BreakRuler(RulerEvent __instance, MethodBase __originalMethod, ref bool ___active, ref IEnumerator ___eventTimer)
        {
            if (TeacherManager.Instance?.MainTeacherPrefab == null || TeacherManager.DefaultBaldiEnabled) return true;
            AccessTools.MethodDelegate<Action>(_Begin, __instance, false).Invoke();
            TeacherManager.Instance?.DoIfMainTeacher(t => t.BreakRuler());
            return false;
        }
        [HarmonyPatch(nameof(RulerEvent.End)), HarmonyPrefix]
        public static bool RestoreRuler(RulerEvent __instance, ref bool ___active, ref EnvironmentController ___ec)
        {
            if (TeacherManager.Instance?.MainTeacherPrefab == null || TeacherManager.DefaultBaldiEnabled) return true;
            AccessTools.MethodDelegate<Action>(_End, __instance, false).Invoke();
            TeacherManager.Instance?.DoIfMainTeacher(t => t.RestoreRuler());
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.PleaseBaldi))]
    internal class PleaseTeacher
    {
        internal static bool Prefix(float time, bool rewardSticker, EnvironmentController ___ec)
        {
            if (TeacherManager.Instance == null) return true;
            foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
            {
                var prevstate = teacher.behaviorStateMachine.currentState;
                var praisestate = teacher.GetPraiseState(time, prevstate);
                if (prevstate.GetType() != praisestate.GetType() && prevstate.GetType() != teacher.GetHappyState().GetType())
                    teacher.behaviorStateMachine.ChangeState(praisestate);
            }
            foreach (var npc in ___ec.Npcs)
            {
                if (npc.Character == Character.Baldi && npc is not Teacher)
                    npc.GetComponent<Baldi>().Praise(time, rewardSticker);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Activity), nameof(Activity.Completed), [typeof(int), typeof(bool)])]
    internal class OnActivityAnswer
    {
        internal static void Prefix(int player, bool correct, Activity __instance)
        {
            if (TeacherManager.Instance == null) return;
            foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
            {
                if (correct)
                    teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.GoodMathMachineAnswer(__instance));
                else
                    teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.BadMathMachineAnswer(__instance));
            }
        }
    }
}
