using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeacherAPI.patches;
using TeacherAPI.utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BepInEx.BepInDependency;

namespace TeacherAPI
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", MTM101BaldiDevAPI.VersionNumber)]
    [BepInDependency("mtm101.rulerp.baldiplus.endlessfloors", DependencyFlags.SoftDependency)]
    public class TeacherPlugin : BaseUnityPlugin
    {
        public static TeacherPlugin Instance { get; private set; }

        internal Dictionary<Character, NPC> whoAreTeachers = new Dictionary<Character, NPC>(); // Mostly used to differenciate who are teachers and who are not.
        internal Dictionary<LevelObject, Baldi> originalBaldiPerFloor = new Dictionary<LevelObject, Baldi>();
        public Baldi CurrentBaldi { get; internal set; }

        internal Dictionary<LevelObject, List<WeightedSelection<Teacher>>> potentialTeachers = new Dictionary<LevelObject, List<WeightedSelection<Teacher>>>();
        internal Dictionary<LevelObject, List<WeightedSelection<Teacher>>> potentialAssistants = new Dictionary<LevelObject, List<WeightedSelection<Teacher>>>();
        internal Dictionary<LevelObject, int> floorNumbers = new Dictionary<LevelObject, int>();
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
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAllConditionals();
            GeneratorManagement.Register(this, GenerationModType.Base, EditGenerator);
        }
        private void EditGenerator(string floorName, int floorNumber, SceneObject floorObject)
        {
            if (floorObject.levelObject.potentialBaldis.Length != 1)
                MTM101BaldiDevAPI.CauseCrash(Info, new Exception("There is no exactly one PotentialBaldi for this level. What mod did you have installed ?"));

            potentialAssistants[floorObject.levelObject] = new List<WeightedSelection<Teacher>>();
            potentialTeachers[floorObject.levelObject] = new List<WeightedSelection<Teacher>>();
            floorNumbers[floorObject.levelObject] = floorNumber;

            if (!TeacherAPIConfiguration.EnableBaldi.Value)
            {
                foreach (var baldi in floorObject.levelObject.potentialBaldis)
                {
                    baldi.weight = 0;
                }
                Logger.LogInfo("Set Baldi weight to 0 for this floor");
            }

            if (floorName == "INF")
            {
                // MTM, do you eat clowns at breakfast ? 
                foreach (var baldi in floorObject.levelObject.potentialBaldis)
                {
                    baldi.weight = TeacherAPIConfiguration.EnableBaldi.Value ? 100 : 0;
                }
            }

            if (!originalBaldiPerFloor.ContainsKey(floorObject.levelObject))
                originalBaldiPerFloor.Add(floorObject.levelObject, GetPotentialBaldi(floorObject.levelObject));
        }

        internal Baldi GetPotentialBaldi(LevelObject floorObject)
        {
            if (floorObject.potentialBaldis.Count() <= 0)
            {
                Log.LogWarning("potentialBaldis in " + floorObject.name + "is blank!");
                return originalBaldiPerFloor[floorObject];
            }
            var baldis = (from x in floorObject.potentialBaldis
                          where x.selection.GetType().Equals(typeof(Baldi))
                          select (Baldi)x.selection).ToArray();
            if (baldis.Count() > 1)
            {
                (from baldi in baldis select baldi.name).Print("Baldis", TeacherPlugin.Log);
                MTM101BaldiDevAPI.CauseCrash(Info, new Exception("Multiple Baldis found in " + floorObject.name + "!"));
            }
            else if (baldis.Count() <= 0)
            {
                Log.LogWarning("No Baldi found in " + floorObject.name + "!");
                return null;
            }
            return baldis.First();
        }

        /// <summary>
        /// Make your teacher known to TeacherAPI
        /// </summary>
        /// <param name="teacher"></param>
        public static void RegisterTeacher(Teacher teacher)
        {
            teacher.ReflectionSetVariable("ignorePlayerOnSpawn", true); // Or else, the teacher won't spawn instantly.
            Instance.whoAreTeachers.Add(teacher.Character, teacher);
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
        public static bool IsEndlessFloorsLoaded()
        {
            return (
                from x in Chainloader.PluginInfos
                where x.Key.Equals("mtm101.rulerp.baldiplus.endlessfloors")
                select x.Key
            ).Count() > 0;
        }

        /// <summary>
        /// Load textures from a pattern, used to easily load animations.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="pattern">A pattern that will go through String.Format(pattern, i)</param>
        /// <param name="range"></param>
        /// <returns></returns>
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

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.teacherapi";
        public const string PLUGIN_NAME = "Teacher API";
        public const string PLUGIN_VERSION = "0.1.3";
    }
}
