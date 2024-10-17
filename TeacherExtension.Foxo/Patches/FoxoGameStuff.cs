using HarmonyLib;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TeacherAPI;
using TeacherExtension.Foxo.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeacherExtension.Foxo.Patches
{
    [HarmonyPatch]
    class GameManStuff
    {
        [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.EndGame)), HarmonyPostfix]
        static void DeathCounterIncrease() => FoxoPlugin.Instance.deathCounter.deaths++;

        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.Initialize)), HarmonyPostfix]
        static void SwapApplesAndExtinguishers(BaseGameManager __instance)
        {
            if (TeacherManager.Instance?.SpawnedMainTeacher == null) return;
            var mainteach = AccessTools.DeclaredField(typeof(TeacherManager), "<MainTeacherPrefab>k__BackingField").GetValue(TeacherManager.Instance) as Teacher; // I took it from UnityExplorer
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                var player = CoreGameManager.Instance.GetPlayer(i);
                CoreGameManager.Instance.GetPlayer(i).itm.items.DoIf(x => x.itemType == global::Items.Apple, x =>
                {
                    if (mainteach?.GetComponent<Foxo>() != null)
                        player.itm.SetItem(ItemMetaStorage.Instance.Find(f => f.value.item.GetComponent<FireExtinguisher>() != null).value, player.itm.items.ToList().IndexOf(x));
                    else
                        player.itm.SetItem(ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value, player.itm.items.ToList().IndexOf(x));
                });
            }
            foreach (var item in __instance.Ec.items)
            {
                if (item.item.itemType != global::Items.Apple) continue;
                if (mainteach?.GetComponent<Foxo>() != null)
                    item.AssignItem(ItemMetaStorage.Instance.Find(f => f.value.item.GetComponent<FireExtinguisher>() != null).value);
                else
                    item.AssignItem(ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value);
            }
        }
        [HarmonyPatch(typeof(Pickup), nameof(Pickup.AssignItem)), HarmonyPrefix]
        static bool AssignAppleReplacement(ItemObject item, Pickup __instance)
        {
            if (TeacherManager.Instance?.SpawnedMainTeacher == null) return true;
            if (item == ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value
                && TeacherManager.Instance?.SpawnedMainTeacher?.GetComponent<Foxo>() != null)
            {
                __instance.AssignItem(ItemMetaStorage.Instance.Find(f => f.value.item.GetComponent<FireExtinguisher>() != null).value);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(PartyEvent), nameof(PartyEvent.Begin)), HarmonyPostfix]
        static void WhyTheFix(ref Pickup ___currentPickup)
        {
            if (TeacherManager.Instance == null || TeacherManager.Instance?.SpawnedMainTeacher == null) return;
            if (___currentPickup.item == ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value
                && TeacherManager.Instance?.SpawnedMainTeacher?.GetComponent<Foxo>() != null)
                ___currentPickup.AssignItem(ItemMetaStorage.Instance.Find(f => f.value.item.GetComponent<FireExtinguisher>() != null).value);
        }

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

    [HarmonyPatch(typeof(SubtitleController), nameof(SubtitleController.Initialize)), HarmonyPriority(Priority.Last)]
    class FoxoFont
    {
        static void Prefix(SubtitleController __instance)
        {
            if (Foxo.audios.GetAll<SoundObject>().Contains(__instance.soundObject))
                __instance.text.font = Foxo.fonts.Get<TMP_FontAsset>("Cooper24");
        }
    }
}
