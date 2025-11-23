using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(BaseGameManager), "PrepareToLoad")]
    class NoMoreSound
    {
        static void Postfix()
        {
            CoreGameManager.Instance.musicMan.FlushQueue(true);
        }
    }

    [HarmonyPatch]
    class BaldicatorStuff
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.ReInit)), HarmonyPostfix]
        static void CustomBaldicatorRemoval(HudManager __instance)
        {
            foreach (CustomBaldicator baldicator in CustomBaldicator.baldicators) {
                GameObject.Destroy(baldicator.gameObject);
            }
        }
    }
}
