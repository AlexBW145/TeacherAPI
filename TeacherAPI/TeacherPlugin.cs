using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TeacherAPI
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", MTM101BaldiDevAPI.VersionNumber)]
    [BepInDependency("alexbw145.baldiplus.pinedebug", BepInDependency.DependencyFlags.SoftDependency)]
    public class TeacherPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.teacherapi";
        private const string PLUGIN_NAME = "Teacher API";
        private const string PLUGIN_VERSION = "0.2.5";
        public static TeacherPlugin Instance { get; private set; }

        internal readonly Dictionary<Character, NPC> whoAreTeachers = new Dictionary<Character, NPC>(); // Mostly used to differenciate who are teachers and who are not.
        public static ManualLogSource Log { get => Instance.Logger; }

        internal void Awake()
        {
            Instance = this;
            TeacherAPIConfiguration.Setup();
            MTM101BaldiDevAPI.AddWarningScreen(@"<color=blue>TeacherAPI</color> is still a <color=yellow>prototype</color> and you will see unexpected things!</color>

Please read the instructions to report any bugs in the mod page!
If you encounter an error, send me the Logs!", false);
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

                /*potentialAssistants[floorObject.levelObject] = new List<WeightedSelection<Teacher>>();
                potentialTeachers[floorObject.levelObject] = new List<WeightedSelection<Teacher>>();
                floorNumbers[floorObject.levelObject] = floorNumber;*/

                levelObject.SetCustomModValue(Info, "TeacherAPI_PotentialTeachers", new List<WeightedTeacher>());
                levelObject.SetCustomModValue(Info, "TeacherAPI_PotentialAssistants", new List<WeightedTeacher>());
                levelObject.MarkAsModifiedByMod(Info);
            }
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
