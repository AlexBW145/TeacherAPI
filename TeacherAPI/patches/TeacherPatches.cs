using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using System;
using System.Linq;
using UnityEngine;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.GetBaldi))]
    [HarmonyPriority(Priority.First)]
    internal class GetBaldiPatch
    {
        public static void Postfix(ref Baldi __result)
        {
            if (TeacherManager.Instance == null) return;
            if (__result == null && TeacherManager.Instance.SpawnedMainTeacher != null)
            {
                __result = (Baldi)TeacherManager.Instance.SpawnedMainTeacher;
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.BeginSpoopMode))]
    internal class BeginSpoopmodePatch
    {
        internal static bool Prefix()
        {
            if (TeacherManager.Instance == null) return true;
            TeacherManager.Instance.SpoopModeActivated = true;
            return true;
        }
    }

    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.SpawnNPC))]
    internal class ChangeStateAfterTeacherSpawn
    {
        internal static void Postfix()
        {
            if (TeacherManager.Instance == null) return;
            foreach (var teacher in TeacherManager.Instance.spawnedTeachers.Where(x => !x.HasInitialized))
            {
                var mainTeacherPrefab = TeacherManager.Instance.MainTeacherPrefab;
                if (mainTeacherPrefab != null && TeacherManager.Instance.SpawnedMainTeacher == null)
                {
                    if (mainTeacherPrefab.GetType().Equals(teacher.GetType()))
                    {
                        TeacherManager.Instance.SpawnedMainTeacher = teacher;
                    }
                }
                teacher.behaviorStateMachine.ChangeState(TeacherManager.Instance.SpoopModeActivated ? teacher.GetAngryState() :  teacher.GetHappyState());
                teacher.HasInitialized = true;
            }
        }
    }

    internal class ReplaceHappyBaldiWithTeacherPatch
    {
        internal static void ReplaceHappyBaldi(BaseGameManager __instance)
        {
            if (TeacherManager.DefaultBaldiEnabled || TeacherManager.Instance == null) return;
            var happyBaldi = __instance.Ec.gameObject.GetComponentInChildren<HappyBaldi>();
            var teacherManager = __instance.Ec.gameObject.GetComponent<TeacherManager>();

            // The main teacher
            if (teacherManager.MainTeacherPrefab)
            {
                var happyBaldiPos = __instance.Ec.CellFromPosition(happyBaldi.transform.position).position;
                __instance.Ec.SpawnNPC(teacherManager.MainTeacherPrefab, happyBaldiPos);
                TeacherNotebook.RefreshNotebookText();

                GameObject.Destroy(happyBaldi.gameObject);
            }

            
            foreach (var prefab in teacherManager.assistingTeachersPrefabs)
            {
                var cells = __instance.Ec.notebooks
                    .Where(n => n.gameObject.GetComponent<TeacherNotebook>().character == prefab.Character)
                    .Select(n => n.activity.room.RandomEntitySafeCellNoGarbage())
                    .ToArray();
                var i = teacherManager.controlledRng.Next(cells.Count());
                try
                {
                    __instance.Ec.SpawnNPC(prefab, cells[i].position);
                } catch
                {
                    TeacherPlugin.Log.LogError($"Can't spawn {EnumExtensions.GetExtendedName<Character>((int)prefab.Character)} because no notebooks have been assigned.");
                    __instance.Ec.Npcs.DoIf(x => x.GetType().Equals(prefab), (npc) => npc.Despawn());
                    break;
                }
            }

            foreach (var notebook in __instance.Ec.notebooks)
            {
                var teacherNotebook = notebook.gameObject.GetComponent<TeacherNotebook>();
                if (teacherNotebook.character != teacherManager.MainTeacherPrefab.Character) 
                    notebook.Hide(true);
            }
        }

        [HarmonyPatch]
        internal class ManagerPatches
        {
            [HarmonyPatch(typeof(MainGameManager), "CreateHappyBaldi")]
            [HarmonyPatch(typeof(EndlessGameManager), "CreateHappyBaldi")]
            [HarmonyPostfix]
            internal static void InGame(object __instance)
            {
                ReplaceHappyBaldi(__instance as BaseGameManager);
            }

            [HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.BeginPlay))]
            [HarmonyPatch(typeof(EndlessGameManager), nameof(EndlessGameManager.BeginPlay))]
            [HarmonyPostfix]
            static void ReplaceMusic()
            {
                if (TeacherManager.Instance == null) return;
                var replacement = TeacherManager.Instance.SpawnedMainTeacher.ReplacementMusic;
                if (replacement.GetType().Equals(typeof(string)))
                {
                    string str = replacement as string;
                    if (str.ToLower() == "mute")
                    {
                        MusicManager.Instance.StopMidi();
                        return;
                    }
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        MusicManager.Instance.StopMidi();
                        MusicManager.Instance.PlayMidi(str, true);
                        return;
                    }
                }
                else if (replacement.GetType().Equals(typeof(SoundObject)))
                {
                    SoundObject snd = replacement as SoundObject;
                    MusicManager.Instance.StopMidi();
                    CoreGameManager.Instance.musicMan.QueueAudio(snd, true);
                    CoreGameManager.Instance.musicMan.SetLoop(true);
                    return;
                }
            }
        }

        
    }

    [HarmonyPatch(typeof(RulerEvent), nameof(RulerEvent.Begin))]
    internal class BreakRulerPatch
    {
        public static void Postfix()
        {
            TeacherManager.Instance?.DoIfMainTeacher(t => t.BreakRuler());
        }
    }

    [HarmonyPatch(typeof(RulerEvent), nameof(RulerEvent.End))]
    internal class RestoreRulerPatch
    {
        public static void Postfix()
        {
            TeacherManager.Instance?.DoIfMainTeacher(t => t.RestoreRuler());
        }
    }

    /*[HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.AngerBaldi))]
    internal class EndlessAnger
    {
        public static void Postfix(float val, BaseGameManager __instance)
        {
            foreach (NPC npc in __instance.Ec.Npcs)
                if (npc.GetComponent<Teacher>() && npc.GetComponent<Teacher>()?.behaviorStateMachine.currentState.GetType() != npc.GetComponent<Teacher>()?.GetHappyState().GetType())
                    npc.GetComponent<Teacher>().GetAngry(val);
        }
    }*/
}
