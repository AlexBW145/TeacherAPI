using BaldiTVAnnouncer;
using BaldiTVAnnouncer.Patches;
using HarmonyLib;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeacherAPI;
using UnityEngine;
using MTM101BaldAPI;
using System.Reflection;

namespace TeacherExtension.Baldimore;

internal class AnnouncerTVFixer
{
    [Obsolete("Announcer Baldi now uses a prefab instead of changing Baldi NPC completely", true)]
    internal static void BugfixForBaldiTVAnnouncement(TeacherBaldi baldi, Baldi refBaldi)
    {
        var hud = Resources.FindObjectsOfTypeAll<CoreGameManager>().Last().ReflectionGetVariable("hudPref") as HudManager;
        var volAnim = hud.BaldiTv.GetComponentInChildren<VolumeAnimator>();
        var rot = baldi.gameObject.AddComponent<AnimatedSpriteRotator>();
        rot.ReflectionSetVariable("spriteMap", refBaldi.GetComponent<AnimatedSpriteRotator>().ReflectionGetVariable("spriteMap"));
        rot.ReflectionSetVariable("renderer", baldi.spriteRenderer[0]);
        rot.targetSprite = refBaldi.GetComponent<AnimatedSpriteRotator>().targetSprite;
        rot.enabled = false;

        var animator = baldi.gameObject.AddComponent<SpriteVolumeAnimator>();
        animator.renderer = rot;
        animator.sensitivity = volAnim.sensitivity;
        animator.enabled = false;
        animator.usesAnimationCurve = true;
        animator.sprites = refBaldi.GetComponent<SpriteVolumeAnimator>().sprites;
        animator.bufferTime = volAnim.bufferTime;
    }

    internal static void UnpatchBaldiTVAnnouncements(Harmony harmony)
    {
        // Cancel the patches because of the way that the functions are done.
        MethodInfo 
            _Update = AccessTools.Method(typeof(BaldiTV), "Update"),
            _QueueEnumerator = AccessTools.Method(typeof(BaldiTV), "QueueEnumerator");
        var announcerGUID = typeof(BaldiTVPatches).Module.ModuleVersionId;
        
        harmony.Unpatch(_Update, Harmony.GetPatchInfo(_Update).Prefixes.First(x => x.PatchMethod.Module.ModuleVersionId == announcerGUID).PatchMethod);
        harmony.Unpatch(_QueueEnumerator, Harmony.GetPatchInfo(_QueueEnumerator).Prefixes.First(x => x.PatchMethod.Module.ModuleVersionId == announcerGUID).PatchMethod);
    }
}

[ConditionalPatchMod("pixelguy.pixelmodding.baldiplus.balditvannouncer"), HarmonyPatch]
class DoNotTheHappyBaldi
{
    [HarmonyPatch(typeof(Baldi_GoToRoom), nameof(Baldi_GoToRoom.Enter)), HarmonyPrefix]
    static bool StateFailsafe(ref Baldi ___baldi, ref NpcState ___previousState, Baldi_GoToRoom __instance, ref bool ___initialized)
    {
        if (___previousState is TeacherBaldi_Happy)
        {
            if (!___initialized)
                __instance.Initialize();
            else
                __instance.Resume();
            ___baldi.behaviorStateMachine.ChangeState(___previousState);
            return false;
        }
        return true;
    }
    // The rest below are just modified versions of the og patches.
    [HarmonyPatch(typeof(BaldiTV), "QueueEnumerator"), HarmonyPrefix]
    static void DoNotPrepareTheHappyBaldi(IEnumerator enumerator)
    {
        if (TeacherManager.Instance == null || BaldiTVObject.availableTVs.Count == 0) return;
        if (enumerator.GetType() == BaldiTVPatches.baldiSpeakType)
        {
            var baldi = BaseGameManager.Instance.Ec.GetBaldi();
            if (baldi == null || baldi.Character != Character.Baldi)
            {
                var list = TeacherManager.Instance.GetTeachersOfType<TeacherBaldi>();
                if (list.Length != 0)
                    baldi = list[0];
            }
            if (baldi && baldi.Character == Character.Baldi) // Very important character check to not mess up TeacherAPI
            {
                if (baldi.behaviorStateMachine.CurrentState is (Baldi_GoBackToTheSpot or not Baldi_Announcer) and not TeacherBaldi_Happy)
                    baldi.behaviorStateMachine.ChangeState(new Baldi_GoToRoom(baldi, baldi, baldi.behaviorStateMachine.CurrentState, BaldiTVPatches.isAnEvent, baldi.transform.position));
            }
            BaldiTVPatches.isAnEvent = false;
        }
    }
    private static FieldInfo _previousState = AccessTools.DeclaredField(typeof(Baldi_SubState), "previousState");
    [HarmonyPatch(typeof(BaldiTV), "Update"), HarmonyPrefix]
    static void IsTeacherBaldiFree(List<IEnumerator> ___queuedEnumerators, bool ___busy)
    {
        if (TeacherManager.Instance == null || !BaseGameManager.Instance || BaldiTVObject.availableTVs.Count == 0) return;

        var baldi = BaseGameManager.Instance.Ec.GetBaldi();
        if (baldi == null || baldi.Character != Character.Baldi)
        {
            var list = TeacherManager.Instance.GetTeachersOfType<TeacherBaldi>();
            if (list.Length != 0)
                baldi = list[0];
        }
        if (!baldi || baldi.Character != Character.Baldi)
            return;

        if (baldi.behaviorStateMachine.CurrentState is not Baldi_Announcer and not TeacherBaldi_Happy)
        {
            if (!___queuedEnumerators.Exists(x => x.GetType() == BaldiTVPatches.baldiSpeakType))
                return;
            baldi.behaviorStateMachine.ChangeState(new Baldi_GoToRoom(baldi, baldi, baldi.behaviorStateMachine.CurrentState, BaldiTVPatches.isAnEvent, baldi.transform.position));
        }


        if (___queuedEnumerators.Count != 0)
        {
            int speakIndex = ___queuedEnumerators.FindIndex(x => x.GetType() == BaldiTVPatches.baldiSpeakType);
            if (baldi.behaviorStateMachine.CurrentState is Baldi_EndSpeaking endSpeak && speakIndex != -1)
                baldi.behaviorStateMachine.ChangeState(new Baldi_Speaking(baldi, baldi, (NpcState)_previousState.GetValue(endSpeak), endSpeak.EventAnnouncement, endSpeak.tvObj, endSpeak.reachedInTime, endSpeak.ogPosition));
            else if (baldi.behaviorStateMachine.CurrentState is Baldi_Speaking speaker)
            {
                if (!___busy && speakIndex == -1)
                {
                    baldi.behaviorStateMachine.ChangeState(new Baldi_EndSpeaking(baldi, baldi, speaker.talkingBaldi, (NpcState)_previousState.GetValue(speaker), speaker.EventAnnouncement, speaker.tvObj, speaker.reachedInTime, speaker.ogPosition));
                    return;
                }
            }

            if (___busy)
            {
                if (baldi.behaviorStateMachine.CurrentState is Baldi_GoToRoom room) // To make sure he doesn't miss it out!
                {
                    if (___queuedEnumerators[0].GetType() == BaldiTVPatches.baldiSpeakType)
                    {
                        baldi.Navigator.Entity.Teleport(room.PosToGo);
                        room.reachedInTime = false;
                        room.DestinationEmpty();
                    }
                }
            }
        }
    }
}