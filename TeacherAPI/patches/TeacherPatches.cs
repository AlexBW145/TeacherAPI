using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.GetBaldi))]
    [HarmonyPriority(Priority.First)]
    internal class GetBaldiPatch
    {
        public static bool Prefix(ref Baldi __result)
        {
            if (TeacherManager.Instance == null) return true;
            if (TeacherManager.Instance.SpawnedMainTeacher != null)
            {
                __result = TeacherManager.Instance.SpawnedMainTeacher;
                return false;
            }
            return true;
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

    [HarmonyPatch(typeof(Activity), nameof(Activity.Completed), [typeof(int), typeof(bool)])]
    class AngeredTeacherByBadSkill
    {
        static void Postfix(int player, bool correct, Activity __instance, Notebook ___notebook)
        {
            if (TeacherManager.DefaultBaldiEnabled || TeacherManager.Instance == null) return;
            var component = ___notebook.GetComponent<TeacherNotebook>();
            if (!correct && component != null)
            {
                foreach (var teacher in TeacherManager.Instance.spawnedTeachers.FindAll(x => x.Character == component.character))
                    teacher?.GetAngry(1f); // Default value from math machine, match machine, and balloon buster.
            }
        }
    }

    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.SpawnNPC))]
    internal class ChangeStateAfterTeacherSpawn
    {
        internal static void Postfix(NPC npc, IntVector2 position, EnvironmentController __instance, ref NPC __result)
        {
            if (TeacherManager.Instance == null || npc is not Teacher) return;
            var teacher = __result as Teacher;
            var mainTeacherPrefab = TeacherManager.Instance.MainTeacherPrefab;
            if (mainTeacherPrefab != null && TeacherManager.Instance.SpawnedMainTeacher == null)
            {
                if (npc == mainTeacherPrefab)
                    TeacherManager.Instance.SpawnedMainTeacher = teacher;
            }
            teacher.behaviorStateMachine.ChangeState(__instance.Active ? teacher.GetAngryState() : teacher.GetHappyState());
            if (__instance.Active)
                teacher.Navigator.Entity.SetInteractionState(true);
            teacher.HasInitialized = true;
            CustomBaldicator.RearrangeBaldicators();
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
            var tileSpawns = __instance.Ec.npcSpawnTile.ToList();

            // The main teacher
            if (teacherManager.MainTeacherPrefab)
            {
                var happyBaldiPos = __instance.Ec.CellFromPosition(happyBaldi.transform.position).position;
                tileSpawns.RemoveAt(__instance.Ec.npcsToSpawn.IndexOf(teacherManager.MainTeacherPrefab));
                __instance.Ec.npcsToSpawn.Remove(teacherManager.MainTeacherPrefab);
                __instance.Ec.SpawnNPC(teacherManager.MainTeacherPrefab, happyBaldiPos);
                TeacherNotebook.RefreshNotebookText();

                GameObject.Destroy(happyBaldi.gameObject);
            }

            foreach (var prefab in teacherManager.assistingTeachersPrefabs)
            {
                tileSpawns.RemoveAt(__instance.Ec.npcsToSpawn.IndexOf(prefab));
                __instance.Ec.npcsToSpawn.Remove(prefab);
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

            __instance.Ec.npcSpawnTile = tileSpawns.ToArray();
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
                if (TeacherManager.Instance == null || TeacherManager.Instance.SpawnedMainTeacher == null) return;
                var replacement = TeacherManager.Instance.SpawnedMainTeacher.ReplacementMusic;
                if (replacement == null) return;
                if (replacement is string)
                {
                    string str = replacement as string;
                    if (str.ToLower() == "mute")
                    {
                        MusicManager.Instance.StopMidi();
                        MusicManager.Instance.Invoke("StopMidi", 0.01f); // Useless strategy is used.
                        return;
                    }
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        MusicManager.Instance.StopMidi();
                        MusicManager.Instance.PlayMidi(str, true);
                        return;
                    }
                }
                else if (replacement is SoundObject)
                {
                    SoundObject snd = replacement as SoundObject;
                    MusicManager.Instance.StopMidi();
                    CoreGameManager.Instance.musicMan.QueueAudio(snd, true);
                    CoreGameManager.Instance.musicMan.SetLoop(true);
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.Initialize)), HarmonyPostfix]
        static void PostcheckInstant(BaseGameManager __instance)
        {
            if (TeacherManager.Instance == null || TeacherManager.DefaultBaldiEnabled) return;
            if (__instance.spawnNpcsOnInit)
                TeacherManager.Instance.SpoopModeActivated = true;
        }
        [HarmonyPatch(typeof(BaseGameManager), "ExitedSpawn"), HarmonyPostfix]
        static void PostcheckWait(BaseGameManager __instance)
        {
            if (TeacherManager.Instance == null || TeacherManager.DefaultBaldiEnabled) return;
            if (__instance.spawnImmediately)
                TeacherManager.Instance.SpoopModeActivated = true;
        }
    }

    [HarmonyPatch]
    internal class RedirectNPCStatesPatch
    {
        [HarmonyPatch(typeof(Baldi_Chase), nameof(Baldi_Chase.Enter))]
        [HarmonyPatch(typeof(Baldi_Chase_Broken), nameof(Baldi_Chase_Broken.Enter))]
        [HarmonyPrefix]
        static bool RedirectChase(Baldi_Chase __instance)
        {
            if (__instance.Npc is Teacher)
            {
                var teacher = __instance.Npc as Teacher;
                teacher.behaviorStateMachine.ChangeState(teacher.GetAngryState());
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(Baldi_Praise), nameof(Baldi_Praise.Enter))]
        [HarmonyPrefix]
        static bool RedirectPraise(Baldi_Praise __instance, float ___time)
        {
            if (__instance.GetType().IsSubclassOf(typeof(Baldi_Praise))) return true; // Do not the locker interaction.
            if (__instance.Npc is Teacher)
            {
                var teacher = __instance.Npc as Teacher;
                teacher.behaviorStateMachine.ChangeState(teacher.GetPraiseState(___time));
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(Baldi), nameof(Baldi.Praise)), HarmonyPrefix]
        static bool UseThatPraise(float time, bool rewardSticker, Baldi __instance)
        {
            if (__instance is Teacher)
            {
                var teacher = __instance as Teacher;
                if (teacher.behaviorStateMachine.currentState.GetType().Equals(teacher.GetHappyState().GetType()))
                    return false;
                __instance.AudMan?.FlushQueue(true);
                float num = 0f;
                if (rewardSticker)
                    num = 3 * StickerManager.Instance.StickerValue(Sticker.BaldiPraise);

                __instance.behaviorStateMachine.ChangeState(new Baldi_Praise(__instance, __instance, __instance.behaviorStateMachine.currentState, time + num));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Baldi), nameof(Baldi.ResetSprite))]
        [HarmonyPatch(typeof(Baldi), nameof(Baldi.SlapNormal))]
        [HarmonyPatch(typeof(Baldi), nameof(Baldi.SlapBroken))]
        [HarmonyPrefix]
        static bool RedirectSlapNormal(Baldi __instance, Animator ___animator, VolumeAnimator ___volumeAnimator)
        {
            if (__instance is Teacher)
                return ___animator != null && ___volumeAnimator != null;
            return true;
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
