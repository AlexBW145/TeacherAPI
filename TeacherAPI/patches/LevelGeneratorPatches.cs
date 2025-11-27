using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TeacherAPI.utils;

namespace TeacherAPI.patches
{
    [HarmonyPatch(typeof(LevelGenerator))]
    class LevelGeneratorPatch
    {
        [HarmonyPatch(nameof(LevelGenerator.StartGenerate), MethodType.Normal), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TeacherManagerInitalize(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
            .Start()
            .MatchForward(true,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(CodeInstruction.Call(typeof(LevelBuilder), nameof(LevelBuilder.StartGenerate)))
            ).ThrowIfInvalid($"TeacherAPI failed to patch {nameof(LevelGenerator.StartGenerate)} due to invalid opcode matching, did something go wrong now??").Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
            Transpilers.EmitDelegate<Action<LevelGenerator>>((__instance) =>
            {
                if (__instance.ld is not CustomLevelGenerationParameters)
                {
                    TeacherManager.DefaultBaldiEnabled = true;
                    return;
                }

                var ld = __instance.ld as CustomLevelGenerationParameters;
                var man = __instance.Ec.gameObject.AddComponent<TeacherManager>();
                __instance.controlledRNG = new System.Random(CoreGameManager.Instance.Seed() + __instance.scene.levelNo);
                man.Initialize(__instance);
                TeacherPlugin.Instance.CurrentBaldi = TeacherPlugin.Instance.GetPotentialBaldi(ld);

                TeacherManager.DefaultBaldiEnabled = TeacherPlugin.Instance.CurrentBaldi == null || TeacherAPIConfiguration.EnableBaldi.Value ||
                    ld.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialTeachers") == null || ld.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialAssistants") == null ||
                    ((List<WeightedTeacher>)ld.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialTeachers")).Count == 0;
                if (TeacherManager.DefaultBaldiEnabled) return;

                List<WeightedSelection<Teacher>> potentialTeachers = WeightedTeacher.Convert(ld.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialTeachers") as List<WeightedTeacher>);
                List<WeightedSelection<Teacher>> potentialAssistants = WeightedTeacher.Convert(ld.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialAssistants") as List<WeightedTeacher>);
                var mainTeacher = WeightedSelection<Teacher>.ControlledRandomSelectionList(potentialTeachers, __instance.controlledRNG);
                man.MainTeacherPrefab = mainTeacher;
                potentialTeachers.PrintWeights("Potential Teachers", TeacherPlugin.Log);
                TeacherPlugin.Log.LogInfo($"Selected Main Teacher {EnumExtensions.GetExtendedName<Character>((int)mainTeacher.Character)}");

                // Assistants setup
                var policy = mainTeacher.GetAssistantPolicy();
                var assistants = potentialAssistants
                    .Where(t => t.selection != man.MainTeacherPrefab)
                    .Where(t => policy.CheckAssistant(t.selection))
                    .ToList();

                assistants.PrintWeights("Potential Assistants", TeacherPlugin.Log);

                for (var x = 0; x < policy.maxAssistants; x++)
                {
                    if (assistants.Count > 0 && __instance.controlledRNG.NextDouble() < (double)policy.probability && !TeacherAPIConfiguration.DisableAssistingTeachers.Value)
                    {
                        var i = WeightedSelection<Teacher>.ControlledRandomIndex(assistants.ToArray(), __instance.controlledRNG);
                        TeacherPlugin.Log.LogInfo($"Selected Teacher {EnumExtensions.GetExtendedName<Character>((int)assistants[i].selection.Character)}");
                        man.assistingTeachersPrefabs.Add(assistants[i].selection);
                        assistants.Remove(assistants[i]);
                    }
                }

                ld.potentialBaldis = new WeightedNPC[] { }; // Don't put anything in EC.NPCS, only secondary teachers can be there.
                                                            
                ld.forcedNpcs = ld.forcedNpcs.AddToArray(man.MainTeacherPrefab); // Because the new level generator parameters exists.
                ld.forcedNpcs = ld.forcedNpcs.AddRangeToArray(man.assistingTeachersPrefabs.ToArray()); // Their posters will generate regardless if we are patching the character posters room function.
            }))
            .InstructionEnumeration();
        // See above, useless.
        /*[HarmonyPatch(typeof(CharacterPostersRoomFunction), nameof(CharacterPostersRoomFunction.Build)), HarmonyPrefix]
        static bool JustNotAddInNPCs() => TeacherManager.DefaultBaldiEnabled || !TeacherManager.Instance.MainTeacherPrefab.disableNpcs;
        [HarmonyPatch(typeof(CharacterPostersRoomFunction), nameof(CharacterPostersRoomFunction.Build)), HarmonyPostfix]
        static void JustAddinEmPosters(LevelBuilder builder, System.Random rng, CharacterPostersRoomFunction __instance)
        {
            if (TeacherManager.DefaultBaldiEnabled)
                return;
            List<Cell> tilesOfShape = __instance.Room.GetTilesOfShape(TileShapeMask.Single | TileShapeMask.Corner, true);
            List<PosterObject> list = new List<PosterObject>();
            list.Add(TeacherManager.Instance.MainTeacherPrefab.Poster);
            foreach (var assistant in TeacherManager.Instance.assistingTeachersPrefabs)
                list.Add(assistant.Poster);
            for (int i = 0; i < tilesOfShape.Count; i++)
            {
                if (!tilesOfShape[i].HasSoftFreeWall)
                {
                    tilesOfShape.RemoveAt(i);
                    i--;
                }
            }
            while (tilesOfShape.Count > 0 && list.Count > 0)
            {
                int num = rng.Next(0, tilesOfShape.Count);
                int num2 = rng.Next(0, list.Count);
                __instance.Room.ec.BuildPoster(list[num2], tilesOfShape[num], tilesOfShape[num].RandomUncoveredDirection(rng));
                list.RemoveAt(num2);
                if (!tilesOfShape[num].HasSoftFreeWall)
                {
                    tilesOfShape.RemoveAt(num);
                }
            }
        }*/
        /*[HarmonyPatch(nameof(LevelGenerator.Generate), MethodType.Enumerator), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ClearTeachersFromForcedIf(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
            .End()
            .MatchBack(false,
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Ldc_I4_0),
            new CodeMatch(CodeInstruction.StoreField(typeof(LevelBuilder), nameof(LevelBuilder.levelInProgress))),
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Ldc_I4_1),
            new CodeMatch(CodeInstruction.StoreField(typeof(LevelBuilder), nameof(LevelBuilder.levelCreated)))
            ).ThrowIfInvalid($"TeacherAPI failed to patch {nameof(LevelGenerator.Generate)} due to invalid opcode matching, did something go wrong now??")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
            Transpilers.EmitDelegate<Action<LevelGenerator>>((__instance) =>
            {
                if (TeacherManager.DefaultBaldiEnabled) return;
                TeacherManager teacherMan = __instance.Ec.GetComponent<TeacherManager>();
                if (teacherMan == null) return;
                // Was once in `ReplaceHappyBaldiWithTeacherPatch.ReplaceHappyBaldi`, got moved to here because of BBT's Baldi's Yearbook.
                if (BaseGameManager.Instance is MainGameManager || BaseGameManager.Instance is EndlessGameManager)
                {
                    __instance.Ec.npcsToSpawn.Remove(teacherMan.MainTeacherPrefab);
                    foreach (var prefab in teacherMan.assistingTeachersPrefabs)
                        __instance.Ec.npcsToSpawn.Remove(prefab);
                }
            })).InstructionEnumeration();*/

        /*internal static void Postfix(LevelGenerator __instance, ref IEnumerator __result)
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
        }*/

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
    }
}
