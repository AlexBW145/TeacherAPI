using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
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

    [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.AngerBaldi))]
    class NoSharedOrSameEnumAnger
    {
        internal static bool Prefix() => TeacherManager.Instance == null;
    }

    [HarmonyPatch(typeof(Activity), nameof(Activity.Completed), new Type[] { typeof(int), typeof(bool) })]
    class AngeredTeacherByBadSkill
    {
        static void Postfix(int player, bool correct, Activity __instance, Notebook ___notebook)
        {
            if (TeacherManager.DefaultBaldiEnabled || TeacherManager.Instance == null) return;
            var component = ___notebook.GetComponent<TeacherNotebook>();
            if (!correct && component != null)
            {
                var teacher = TeacherManager.Instance.spawnedTeachers.Find(x => x.Character == component.character);
                teacher?.GetAngry(1f);
            }
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
        private static FieldInfo _activity = AccessTools.DeclaredField(typeof(RoomController), "activity");
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
                    .SelectMany(n => n.activity.room.AllEntitySafeCellsNoGarbage()).ToList();
                var doors = new List<Door>(cells.SelectMany(x => x.room.doors));
                var notebooks = __instance.Ec.notebooks
                    .Where(n => n.gameObject.GetComponent<TeacherNotebook>().character == prefab.Character).ToList();
                for (int cell = cells.Count - 1; cell >= 0; cell--)
                {
                    var notebookPos = __instance.Ec.CellFromPosition(notebooks.Find(notebook => (Activity)_activity.GetValue(cells[cell].room) == notebook.activity).transform.position);
                    if (cells[cell].shape == TileShapeMask.Open // But why??
                        || cells[cell].room.size.x <= 4
                        || cells[cell].room.size.z <= 4
                        || __instance.Ec.GetDistance(notebookPos, cells[cell]) <= 24)
                    {
                        cells.RemoveAt(cell);
                        continue;
                    }
                    for (int j = 0; j < doors.Count; j++)
                    {
                        if (cells[cell].room.doors.Contains(doors[j]) && 
                            (__instance.Ec.GetDistance(doors[j].aTile, cells[cell]) <= 30 || cells[cell].HasWallInDirection(doors[j].direction)))
                        {
                            cells.RemoveAt(cell);
                            break;
                        }
                    }
                }
                if (cells.Count == 0) // Failsafe
                    cells.AddRange(__instance.Ec.rooms.Where(x => x.category == RoomCategory.Faculty).SelectMany(x => x.AllEntitySafeCellsNoGarbage()));
                if (notebooks.Count > 0)
                {
                    if (cells.Count != 0)
                    {
                        var i = teacherManager.controlledRng.Next(cells.Count);
                        __instance.Ec.SpawnNPC(prefab, cells[i].position);
                    }
                    else
                        TeacherPlugin.Log.LogWarning($"Can't spawn {EnumExtensions.GetExtendedName<Character>((int)prefab.Character)} because there are no cells that are not near the classroom doors and activities and also the lack of faculty rooms.");
                }
                else
                    TeacherPlugin.Log.LogWarning($"Can't spawn {EnumExtensions.GetExtendedName<Character>((int)prefab.Character)} because no notebooks have been assigned.");
            }

            /*foreach (var notebook in __instance.Ec.notebooks)
            {
                var teacherNotebook = notebook.gameObject.GetComponent<TeacherNotebook>();
                if (teacherNotebook.character != teacherManager.MainTeacherPrefab.Character) 
                    notebook.Hide(true);
            }*/

            CustomBaldicator.RearrangeBaldicators();
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
                if (replacement == null) return;
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
