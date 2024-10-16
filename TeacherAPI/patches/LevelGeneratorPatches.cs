﻿using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TeacherAPI.utils;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(LevelGenerator))]
    internal class LevelGeneratorPatches
    {
        /*[HarmonyPatch(nameof(LevelGenerator.StartGenerate))]
        private static void Prefix(LevelGenerator __instance) => instance = __instance;
        private static LevelGenerator instance;

        [HarmonyPatch(nameof(LevelGenerator.Generate), MethodType.Enumerator), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ManagerInitalize(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.ToString))),
                new CodeMatch(OpCodes.Stelem_Ref),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat))),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Debug), nameof(UnityEngine.Debug.Log)))
            )
            .InsertAndAdvance(Transpilers.EmitDelegate<Action>(DoManagerStuff))
            .InstructionEnumeration();

        static void DoManagerStuff()
        {
            var seed = CoreGameManager.Instance.Seed();
            var man = instance.Ec.gameObject.AddComponent<TeacherManager>();
            man.Initialize(instance.Ec, seed);
            TeacherPlugin.Instance.CurrentBaldi = TeacherPlugin.Instance.GetPotentialBaldi(instance.ld);
            if (TeacherPlugin.Instance.potentialTeachers[instance.ld].Count <= 0 || TeacherPlugin.Instance.CurrentBaldi == null)
            {
                TeacherManager.DefaultBaldiEnabled = true;
                return;
            };
            TeacherManager.DefaultBaldiEnabled = false;

            var rng = new System.Random(seed + TeacherPlugin.Instance.floorNumbers[instance.ld]);

            var mainTeacher = WeightedSelection<Teacher>.ControlledRandomSelectionList(TeacherPlugin.Instance.potentialTeachers[instance.ld], rng);
            man.MainTeacherPrefab = mainTeacher;
            TeacherPlugin.Instance.potentialTeachers[instance.ld].PrintWeights("Potential Teachers", TeacherPlugin.Log);
            TeacherPlugin.Log.LogInfo($"Selected Main Teacher {EnumExtensions.GetExtendedName<Character>((int)mainTeacher.Character)}");

            // Assistants setup
            var policy = mainTeacher.GetAssistantPolicy();
            var potentialAssistants = TeacherPlugin.Instance.potentialAssistants[instance.ld]
                .Where(t => t.selection != man.MainTeacherPrefab)
                .Where(t => policy.CheckAssistant(t.selection))
                .ToList();

            potentialAssistants.PrintWeights("Potential Assistants", TeacherPlugin.Log);

            for (var x = 0; x < policy.maxAssistants; x++)
            {
                if (potentialAssistants.Count > 0 && rng.NextDouble() <= policy.probability && !TeacherAPIConfiguration.DisableAssistingTeachers.Value)
                {
                    var i = WeightedSelection<Teacher>.ControlledRandomIndex(potentialAssistants.ToArray(), rng);
                    TeacherPlugin.Log.LogInfo($"Selected Teacher {EnumExtensions.GetExtendedName<Character>((int)potentialAssistants[i].selection.Character)}");
                    man.assistingTeachersPrefabs.Add(potentialAssistants[i].selection);
                    potentialAssistants.Remove(potentialAssistants[i]);
                }
            }

            instance.ld.potentialBaldis = new WeightedNPC[] { }; // Don't put anything in EC.NPCS, only secondary teachers can be there.
        }

        [HarmonyPatch(nameof(LevelGenerator.Generate)), HarmonyPostfix]
        internal static void AddPosters(LevelGenerator __instance, ref IEnumerator __result)
        {
            var seed = CoreGameManager.Instance.Seed();
            var man = TeacherManager.Instance;
            void postfix()
            {
                if (TeacherManager.DefaultBaldiEnabled || TeacherPlugin.Instance.CurrentBaldi == null) return;
                var controlledRng = new System.Random(seed);
                __instance.Ec.offices
                    .ForEach(office => __instance.Ec.BuildPosterInRoom(office, man.MainTeacherPrefab.Poster, controlledRng));
                foreach (var assistant in man.assistingTeachersPrefabs)
                {
                    __instance.Ec.offices
                        .ForEach(office => __instance.Ec.BuildPosterInRoom(office, assistant.Poster, controlledRng));
                }
            }

            var routine = new SimpleEnumerator(__result) { postfixAction = postfix };
            __result = routine.GetEnumerator();
        }*/

        [HarmonyPatch(nameof(LevelGenerator.Generate)), HarmonyPostfix]
        internal static void ManagerInitalize(LevelGenerator __instance, ref IEnumerator __result)
        {
            var seed = CoreGameManager.Instance.Seed();
            var man = __instance.Ec.gameObject.AddComponent<TeacherManager>();
            man.Initialize(__instance.Ec, seed);
            TeacherPlugin.Instance.CurrentBaldi = TeacherPlugin.Instance.GetPotentialBaldi(__instance.ld);

            object itemAction(object obj)
            {
                if (man.MainTeacherPrefab != null) return obj;
                if (TeacherPlugin.Instance.CurrentBaldi == null || !TeacherPlugin.Instance.potentialTeachers.Keys.Contains(__instance.ld) || TeacherPlugin.Instance.potentialTeachers[__instance.ld].Count <= 0)
                {
                    TeacherManager.DefaultBaldiEnabled = true;
                    return obj;
                };
                TeacherManager.DefaultBaldiEnabled = false;

                var rng = new System.Random(seed + TeacherPlugin.Instance.floorNumbers[__instance.ld]);

                var mainTeacher = WeightedSelection<Teacher>.ControlledRandomSelectionList(TeacherPlugin.Instance.potentialTeachers[__instance.ld], rng);
                man.MainTeacherPrefab = mainTeacher;
                TeacherPlugin.Instance.potentialTeachers[__instance.ld].PrintWeights("Potential Teachers", TeacherPlugin.Log);
                TeacherPlugin.Log.LogInfo($"Selected Main Teacher {EnumExtensions.GetExtendedName<Character>((int)mainTeacher.Character)}");

                // Assistants setup
                var policy = mainTeacher.GetAssistantPolicy();
                var potentialAssistants = TeacherPlugin.Instance.potentialAssistants[__instance.ld]
                    .Where(t => t.selection != man.MainTeacherPrefab)
                    .Where(t => policy.CheckAssistant(t.selection))
                    .ToList();

                potentialAssistants.PrintWeights("Potential Assistants", TeacherPlugin.Log);

                for (var x = 0; x < policy.maxAssistants; x++)
                {
                    if (potentialAssistants.Count > 0 && rng.NextDouble() <= policy.probability && !TeacherAPIConfiguration.DisableAssistingTeachers.Value)
                    {
                        var i = WeightedSelection<Teacher>.ControlledRandomIndex(potentialAssistants.ToArray(), rng);
                        TeacherPlugin.Log.LogInfo($"Selected Teacher {EnumExtensions.GetExtendedName<Character>((int)potentialAssistants[i].selection.Character)}");
                        man.assistingTeachersPrefabs.Add(potentialAssistants[i].selection);
                        potentialAssistants.Remove(potentialAssistants[i]);
                    }
                }

                __instance.ld.potentialBaldis = new WeightedNPC[] { }; // Don't put anything in EC.NPCS, only secondary teachers can be there.

                return obj;
            }

            void postfix()
            {
                if (TeacherManager.DefaultBaldiEnabled || TeacherPlugin.Instance.CurrentBaldi == null) return;
                var controlledRng = new System.Random(seed);
                __instance.Ec.offices
                    .ForEach(office => __instance.Ec.BuildPosterInRoom(office, man.MainTeacherPrefab.Poster, controlledRng));
                foreach (var assistant in man.assistingTeachersPrefabs)
                {
                    __instance.Ec.offices
                        .ForEach(office => __instance.Ec.BuildPosterInRoom(office, assistant.Poster, controlledRng));
                }
            }

            var routine = new SimpleEnumerator(__result) { itemAction = itemAction, postfixAction = postfix };
            __result = routine.GetEnumerator();
        }
    }
}
