using HarmonyLib;
using MTM101BaldAPI.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TeacherExtension.Foxo.Patches
{
    [HarmonyPatch]
    class GameManStuff
    {
        [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.EndGame)), HarmonyPostfix]
        static void DeathCounterIncrease() => FoxoPlugin.Instance.deathCounter.deaths++;

        static FieldInfo ___baldiImage = AccessTools.DeclaredField(typeof(BaldiTV), "baldiImage");
        static FieldInfo ___baldiTvAudioManager = AccessTools.DeclaredField(typeof(BaldiTV), "baldiTvAudioManager");

        [HarmonyPatch(typeof(BaldiTV), "BaldiSpeaks"), HarmonyPostfix]
        static IEnumerator GlitchOut(IEnumerator result, SoundObject sound, BaldiTV __instance)
        {
            var img = ___baldiImage.GetValue(__instance) as Image;
            var audman = ___baldiTvAudioManager.GetValue(__instance) as AudioManager;
            var foxoSprites = Foxo.sprites.GetAll<Sprite[]>().ToList().FindAll(x => x.ToList().Find(f => !f.name.ToLower().Contains("wrath") && !f.name.ToLower().Contains("notebook")));

            if (sound == Foxo.audios.Get<SoundObject>("WrathEventAud"))
            {
                img.GetComponent<Animator>().enabled = false;
                img.GetComponent<VolumeAnimator>().enabled = false;
                while (audman.QueuedAudioIsPlaying && img.enabled)
                {
                    img.sprite = foxoSprites[UnityEngine.Random.RandomRangeInt(0, foxoSprites.Count)].First();
                    yield return null;
                }
            }

            while (result.MoveNext())
            {
                if (sound == Foxo.audios.Get<SoundObject>("WrathEventAud"))
                    img.sprite = foxoSprites[UnityEngine.Random.RandomRangeInt(0, foxoSprites.Count)].First();
                yield return result.Current;
            }

            if (sound == Foxo.audios.Get<SoundObject>("WrathEventAud"))
            {
                img.GetComponent<Animator>().enabled = true;
                img.GetComponent<VolumeAnimator>().enabled = true;
            }
        }
    }
}
