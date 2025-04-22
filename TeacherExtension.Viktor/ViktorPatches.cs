using HarmonyLib;
using MTM101BaldAPI.Components;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TeacherAPI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TeacherExtension.Viktor.Patches;

[HarmonyPatch(typeof(BaldiTV))]
class ViktorTV
{
    static SoundObject ViktorAllNotebooks => ViktorPlugin.viktorAssets["Viktor/LastNotebook"] as SoundObject;
    static FieldInfo ___baldiImage = AccessTools.DeclaredField(typeof(BaldiTV), "baldiImage");
    static FieldInfo ___baldiTvAudioManager = AccessTools.DeclaredField(typeof(BaldiTV), "baldiTvAudioManager");

    /*[HarmonyPatch("BaldiSpeaks"), HarmonyPostfix]
    static IEnumerator InitStuff(IEnumerator result, SoundObject sound, BaldiTV __instance)
    {
        var img = ___baldiImage.GetValue(__instance) as Image;
        var audman = ___baldiTvAudioManager.GetValue(__instance) as AudioManager;

        if (sound == ViktorAllNotebooks)
        {
            img.GetComponent<Animator>().enabled = false;
            img.GetComponent<VolumeAnimator>().enabled = false;
            var anim = img.gameObject.AddComponent<CustomImageAnimator>();
            anim.image = img;
            var vol = img.gameObject.AddComponent<CustomVolumeAnimator>();
            for (int i = 0; i < VanessaPlugin.assetMan.Get<Sprite[]>("JennyLiveTVReaction").ToList().Count; i++)
                anim.animations.Add("JenTalk" + i, new CustomAnimation<Sprite>([VanessaPlugin.assetMan.Get<Sprite[]>("JennyLiveTVReaction")[i]], 0.25f));
            vol.animator = anim;
            vol.audioSource = audman.audioDevice;
            vol.animations = anim.animations.Keys.ToArray();
        }

        while (result.MoveNext())
            yield return result.Current;

        if (sound == ViktorAllNotebooks)
        {
            GameObject.Destroy(img.GetComponent<CustomVolumeAnimator>());
            GameObject.Destroy(img.GetComponent<CustomImageAnimator>());
            img.GetComponent<Animator>().enabled = true;
            img.GetComponent<VolumeAnimator>().enabled = true;
        }
    }*/

    [HarmonyPatch(typeof(MainGameManager), "AllNotebooks"), HarmonyPrefix]
    static bool ViktorFinalNotebookPrank()
    {
        if (TeacherManager.Instance == null) return true;
        if (TeacherManager.Instance.SpawnedMainTeacher?.GetComponent<Viktor>() != null)
            return TeacherManager.Instance.SpawnedMainTeacher.GetComponent<Viktor>().AllNotebooksPrank;
        return true;
    }
}

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

[HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.CollectNotebooks))]
class PranksOfAllTime
{
    static void Postfix()
    {
        if (TeacherManager.Instance == null) return;
        if (TeacherManager.Instance?.SpawnedMainTeacher?.GetComponent<Viktor>() != null)
        {
            var statebase = TeacherManager.Instance.SpawnedMainTeacher.behaviorStateMachine.CurrentState as Viktor_StateBase;
            statebase.ThePrank();
        }
    }
}
