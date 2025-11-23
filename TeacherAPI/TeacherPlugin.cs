using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TeacherAPI.utils;
using UnityEngine;

namespace TeacherAPI
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", MTM101BaldiDevAPI.VersionNumber)]
    [BepInDependency("alexbw145.baldiplus.arcadeendlessforever", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("alexbw145.baldiplus.pinedebug", BepInDependency.DependencyFlags.SoftDependency)]
    public class TeacherPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.teacherapi";
        private const string PLUGIN_NAME = "Teacher API";
        private const string PLUGIN_VERSION = "0.1.11";
        public static TeacherPlugin Instance { get; private set; }

        internal readonly Dictionary<Character, NPC> whoAreTeachers = new Dictionary<Character, NPC>(); // Mostly used to differenciate who are teachers and who are not.
        //internal Dictionary<LevelObject, Baldi> originalBaldiPerFloor = new Dictionary<LevelObject, Baldi>();
        public Baldi CurrentBaldi { get; internal set; }

        //internal Dictionary<LevelObject, List<WeightedSelection<Teacher>>> potentialTeachers = new Dictionary<LevelObject, List<WeightedSelection<Teacher>>>();
        //internal Dictionary<LevelObject, List<WeightedSelection<Teacher>>> potentialAssistants = new Dictionary<LevelObject, List<WeightedSelection<Teacher>>>();
        //internal Dictionary<LevelObject, int> floorNumbers = new Dictionary<LevelObject, int>();
        public static ManualLogSource Log { get => Instance.Logger; }

        internal void Awake()
        {
            Instance = this;
            TeacherAPIConfiguration.Setup();
            if (!TeacherAPIConfiguration.DebugMode.Value)
            {
                MTM101BaldiDevAPI.AddWarningScreen(@"<color=blue>TeacherAPI</color> is still a <color=yellow>prototype</color> and you will see unexpected things!</color>

Please read the instructions to report any bugs in the mod page!
If you encounter an error, send me the Logs!", false);
            }
            new Harmony(PLUGIN_GUID).PatchAllConditionals();
            GeneratorManagement.Register(this, GenerationModType.Base, EditGenerator);
            CustomOptionsCore.OnMenuInitialize += (__instance, handler) =>
            {
                handler.AddCategory<TeacherAPIConfiguration>("TeacherAPI");
            };
        }
        private void EditGenerator(string floorName, int floorNumber, SceneObject sceneObject)
        {
            foreach (var levelObject in sceneObject.GetCustomLevelObjects())
            {
                if (levelObject.IsModifiedByMod(Info)) continue;
                if (levelObject.potentialBaldis.Length != 1) {
                    Log.LogWarning($"There is no exactly one PotentialBaldi, skipping {levelObject.name}!");
                    break;
                }

                /*potentialAssistants[floorObject.levelObject] = new List<WeightedSelection<Teacher>>();
                potentialTeachers[floorObject.levelObject] = new List<WeightedSelection<Teacher>>();
                floorNumbers[floorObject.levelObject] = floorNumber;*/

                levelObject.SetCustomModValue(Info, "TeacherAPI_PotentialTeachers", new List<WeightedTeacher>());
                levelObject.SetCustomModValue(Info, "TeacherAPI_PotentialAssistants", new List<WeightedTeacher>());

                if (!TeacherAPIConfiguration.EnableBaldi.Value)
                {
                    foreach (var baldi in levelObject.potentialBaldis)
                        baldi.weight = 0;
                    Logger.LogInfo("Set Baldi weight to 0 for this floor");
                }

                if (floorName == "INF")
                {
                    foreach (var baldi in levelObject.potentialBaldis)
                        baldi.weight = TeacherAPIConfiguration.EnableBaldi.Value ? 100 : 0;
                }

                levelObject.SetCustomModValue(Info, "TeacherAPI_OriginalBaldi", GetPotentialBaldi(levelObject));
                levelObject.MarkAsModifiedByMod(Info);
            }
        }

        internal Baldi GetPotentialBaldi(CustomLevelGenerationParameters floorObject)
        {
            if (floorObject.GetCustomModValue(Info, "TeacherAPI_OriginalBaldi") is Baldi)
                return floorObject.GetCustomModValue(Info, "TeacherAPI_OriginalBaldi") as Baldi;
            return GetPotentialBaldi(floorObject as LevelGenerationParameters);
        }

        internal Baldi GetPotentialBaldi(CustomLevelObject floorObject)
        {
            var param = new CustomLevelGenerationParameters();
            param.AssignData(floorObject, new LevelGenerationModifier());
            param.AssignExtraData(floorObject);
            return GetPotentialBaldi(param);
        }

        internal Baldi GetPotentialBaldi(LevelObject floorObject)
        {
            var param = new LevelGenerationParameters();
            param.AssignData(floorObject, new LevelGenerationModifier());
            return GetPotentialBaldi(param);
        }

        internal Baldi GetPotentialBaldi(LevelGenerationParameters floorObject)
        {
            var baldis = (from x in floorObject.potentialBaldis
                          where x.selection.GetType().Equals(typeof(Baldi))
                          select (Baldi)x.selection).ToArray();
            if (baldis.Count() <= 0)
            {
                Log.LogWarning("potentialBaldis in " + floorObject.name + "is blank!");
                return null; // Why did I do that??
            }
            else if (baldis.Count() > 1)
            {
                (from baldi in baldis select baldi.name).Print("Baldis", TeacherPlugin.Log);
                MTM101BaldiDevAPI.CauseCrash(Info, new Exception("Multiple Baldis found in " + floorObject.name + "!"));
            }
            return baldis.First();
        }

        private static FieldInfo _ignorePlayerOnSpawn = AccessTools.DeclaredField(typeof(NPC), "ignorePlayerOnSpawn");

        /// <summary>
        /// Make your teacher known to TeacherAPI
        /// </summary>
        /// <param name="teacher"></param>
        public static void RegisterTeacher(Teacher teacher)
        {
            _ignorePlayerOnSpawn.SetValue(teacher, true); // Or else the teacher won't spawn instantly.
            Instance.whoAreTeachers.Add(teacher.Character, teacher);
            CustomBaldiInteraction.teacherCheck.Add(teacher.Character, new Dictionary<Type, Func<BaldiInteraction, Teacher, bool>>());
            CustomBaldiInteraction.teacherTriggers.Add(teacher.Character, new Dictionary<Type, Action<BaldiInteraction, Teacher>>());
            CustomBaldiInteraction.teacherPayloads.Add(teacher.Character, new Dictionary<Type, Action<BaldiInteraction, Teacher>>());
        }

        /// <summary>
        /// Will show a warning screen telling the user to install the mod correctly
        /// if the folder for the specified plugin is not found in Modded.
        /// </summary>
        public static void RequiresAssetsFolder(BaseUnityPlugin plug)
        {
            string assetsPath = AssetLoader.GetModPath(plug);
            if (!Directory.Exists(assetsPath))
            {
                MTM101BaldiDevAPI.AddWarningScreen(String.Format(@"
The mod <color=blue>{0}</color> must have the assets file in <color=red>StreamingAssets/Modded</color>!</color>

The name of the assets folder must be <color=red>{1}</color>.", Path.GetFileName(plug.Info.Location), plug.Info.Metadata.GUID), true);
            }
        }

        /// <summary>
        /// Returns true if Infinite Floors/Endless Floors is loaded.
        /// </summary>
        /// <returns></returns>
        public static bool IsEndlessFloorsLoaded() => CoreGameManager.Instance?.sceneObject?.levelTitle == "INF";

        /// <summary>
        /// Load textures from a pattern, used to easily load animations.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="pattern">A pattern that will go through String.Format(pattern, i)</param>
        /// <param name="range"></param>
        /// <returns></returns>
        [Obsolete("This is barebones old code, consider using the Dev API's functions which also support spritesheets!", true)]
        public static Texture2D[] TexturesFromMod(BaseUnityPlugin mod, string pattern, (int, int) range)
        {
            var textures = new List<Texture2D>();
            for (int i = range.Item1; i <= range.Item2; i++)
            {
                textures.Add(AssetLoader.TextureFromMod(mod, String.Format(pattern, i)));
            }
            return textures.ToArray();
        }

    }
}
