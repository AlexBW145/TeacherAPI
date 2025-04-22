using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(GlobalCam), nameof(GlobalCam.Transition))]
    internal class SkipTransition
    {
        internal static bool Prefix()
        {
            return !TeacherAPIConfiguration.DebugMode.Value;
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), "PrepareToLoad")]
    class NoMoreSound
    {
        static void Postfix()
        {
            CoreGameManager.Instance.musicMan.FlushQueue(true);
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.ReInit))]
    class CustomBaldicatorRemoval
    {
        static void Postfix(HudManager __instance)
        {
            foreach (CustomBaldicator baldicator in __instance.GetComponentsInChildren<CustomBaldicator>(true)) {
                GameObject.Destroy(baldicator.gameObject);
            }
        }
    }
}
