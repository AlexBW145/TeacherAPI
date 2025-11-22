using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeacherAPI.patches
{
    [HarmonyPatch]
    class BaldicatorStuff
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.ReInit)), HarmonyPostfix]
        static void CustomBaldicatorRemoval(HudManager __instance)
        {
            foreach (CustomBaldicator baldicator in __instance.GetComponentsInChildren<CustomBaldicator>(true)) {
                GameObject.Destroy(baldicator.gameObject);
            }
        }
    }
}
