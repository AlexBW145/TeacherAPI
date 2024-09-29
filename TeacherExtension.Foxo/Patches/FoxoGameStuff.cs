using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TeacherExtension.Foxo.Patches
{
    [HarmonyPatch]
    class GameManStuff
    {
        [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.EndGame)), HarmonyPostfix]
        static void DeathCounterIncrease()
        {
            FoxoPlugin.Instance.deathCounter.deaths++;
        }

        [HarmonyPatch(typeof(BaldiTV), nameof(BaldiTV.AnnounceEvent)), HarmonyPostfix]
        static void GlitchOut(SoundObject sound, BaldiTV __instance, ref Image ___baldiImage, ref AudioManager ___baldiTvAudioManager)
        {
            if (sound == Foxo.audios.Get<SoundObject>("WrathEventAud"))
                __instance.StartCoroutine(randomizeshit(___baldiImage, ___baldiTvAudioManager));
        }

        static IEnumerator randomizeshit(Image baldi, AudioManager audman)
        {
            yield return new WaitUntil(() => baldi.enabled);
            baldi.GetComponent<Animator>().enabled = false;
            baldi.GetComponent<VolumeAnimator>().enabled = false;
            var foxoSprites = Foxo.sprites.GetAll<Sprite[]>().ToList().FindAll(x => x.ToList().Find(f => !f.name.ToLower().Contains("wrath") && x.ToList().Find(n => !n.name.ToLower().Contains("notebook"))));
            while (audman.QueuedAudioIsPlaying && baldi.enabled)
            {
                baldi.sprite = foxoSprites[UnityEngine.Random.RandomRangeInt(0, foxoSprites.Count)].First();
                yield return null;
            }
            baldi.GetComponent<Animator>().enabled = true;
            baldi.GetComponent<VolumeAnimator>().enabled = true;
        }
    }
}
