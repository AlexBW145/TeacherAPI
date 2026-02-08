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
                var currentBaldi = TeacherPlugin.Instance.GetPotentialBaldi(ld);

                TeacherManager.DefaultBaldiEnabled = currentBaldi == null || TeacherAPIConfiguration.EnableBaldi.Value ||
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
    }
}
