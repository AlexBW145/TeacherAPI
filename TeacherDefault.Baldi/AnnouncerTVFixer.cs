using BaldiTVAnnouncer;
using BaldiTVAnnouncer.Patches;
using HarmonyLib;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeacherAPI;
using UnityEngine;
using MTM101BaldAPI;

namespace TeacherExtension.Baldimore;

internal class AnnouncerTVFixer
{
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
}

[ConditionalPatchMod("pixelguy.pixelmodding.baldiplus.balditvannouncer"), HarmonyPatch]
internal class DoNotTheHappyBaldi
{
    [HarmonyPatch(typeof(Baldi_Announcer), nameof(Baldi_Announcer.Enter))]
    [HarmonyPatch(typeof(Baldi_GoToRoom), nameof(Baldi_GoToRoom.Enter))]
    static void Postfix(ref Baldi ___baldi, ref NpcState ___previousState)
    {
        if (___previousState is TeacherBaldi_Happy)
            ___baldi.behaviorStateMachine.ChangeState(___previousState);
    }
}