using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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

    [HarmonyPatch(typeof(BaseGameManager), "WaitToExitSpawn", MethodType.Enumerator)]
    class ElevatorExitPatch // The only patch because the void itself can be overritten by other components.
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
            .End()
            .MatchBack(true,
            new CodeMatch(OpCodes.Ldloc_1),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BaseGameManager), "ExitedSpawn"))).ThrowIfInvalid("TeacherAPI has failed to patch WaitToExitSpawn!").Advance(1)
            .InsertAndAdvance(Transpilers.EmitDelegate<Action>(() =>
            {
                if (TeacherManager.Instance == null) return;
                foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
                    teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.PlayerExitedSpawn()); // It was misleading for some reason...
            })).InstructionEnumeration();
    }

    [HarmonyPatch(typeof(RulerEvent))]
    internal class RulerEventPatches
    {
        private static MethodInfo _Timer = AccessTools.Method(typeof(RandomEvent), "Timer");
        [HarmonyPatch(nameof(RulerEvent.Begin)), HarmonyPrefix]
        public static bool BreakRuler(RulerEvent __instance, ref bool ___active, ref IEnumerator ___eventTimer)
        {
            if (TeacherManager.Instance?.MainTeacherPrefab == null || TeacherManager.DefaultBaldiEnabled) return true;
            ___active = true;
            ___eventTimer = (IEnumerator)_Timer.Invoke(__instance, [__instance.EventTime]);
            __instance.StartCoroutine(___eventTimer);
            TeacherManager.Instance?.DoIfMainTeacher(t => t.BreakRuler());
            return false;
        }
        [HarmonyPatch(nameof(RulerEvent.End)), HarmonyPrefix]
        public static bool RestoreRuler(RulerEvent __instance, ref bool ___active, ref EnvironmentController ___ec)
        {
            if (TeacherManager.Instance?.MainTeacherPrefab == null || TeacherManager.DefaultBaldiEnabled) return true;
            ___active = false;
            ___ec.EventOver(__instance);
            TeacherManager.Instance?.DoIfMainTeacher(t => t.RestoreRuler());
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.PleaseBaldi))]
    internal class OnGoodMathMachineAnswer
    {
        internal static void Prefix(float time, bool rewardSticker)
        {
            if (TeacherManager.Instance == null) return;
            foreach (var teacher in TeacherManager.Instance.spawnedTeachers)
            {
                teacher.behaviorStateMachine.currentState.AsTeacherState().IfSuccess(state => state.GoodMathMachineAnswer(time));
            }
        }
    }
}
