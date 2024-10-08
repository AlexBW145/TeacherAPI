﻿using HarmonyLib;
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
}
