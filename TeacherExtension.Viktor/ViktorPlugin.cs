using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections;
using UnityEngine;
using TeacherAPI;
using MTM101BaldAPI.ObjectCreation;
using System.IO;
using MTM101BaldAPI.Reflection;

namespace TeacherExtension.Viktor
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("alexbw145.baldiplus.teacherapi", BepInDependency.DependencyFlags.HardDependency)]
    public class ViktorPlugin : BaseUnityPlugin
    {
        public static ViktorPlugin Instance { get; private set; } // Remove it if necessary
        internal static AssetManager viktorAssets = new AssetManager();
        internal Viktor Viktor;

        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Instance = this;
            harmony.PatchAllConditionals();
            TeacherPlugin.RequiresAssetsFolder(this);

            ViktorConfiguration.Setup();
            AssetLoader.LocalizationFromFunction((lang) =>
            {
                switch (lang)
                {
                    default:
                        return new System.Collections.Generic.Dictionary<string, string>();
                    case Language.English:
                        return new System.Collections.Generic.Dictionary<string, string>()
                        {
                            { "PRI_ViktorStrobovski1", "Viktor Strobovski" },
                            { "PRI_ViktorStrobovski2", "A masked fellow who we hired as an advanced math teacher!\nDo not question his motives though..." },
                            { "Vfx_Viktor_Intro", "Oh, hello there. My name is Viktor Strobovski, and we're gonna play hide and seek." },
                            { "Vfx_Viktor_Triggered", "...You should have not done that." },
                            { "Vfx_Viktor_Hard1", "Hey, I want to make the game a bit more exciting." },
                            { "Vfx_Viktor_Hard2", "I will walk a bit quieter." },
                            { "Vfx_Viktor_Hard3", "This will be fun, won't it?" },
                            { "Vfx_Viktor_AllNotebooks1", "My congratulations!" },
                            { "Vfx_Viktor_AllNotebooks2", "You collect all 10 notebooks." },
                            { "Vfx_Viktor_AllNotebooks3", "But.." },
                            { "Vfx_Viktor_AllNotebooks4", "I want more fun!" },
                            { "Vfx_Viktor_AllNotebooks5", "COLLECT THE ELEVENTH NOTEBOOK!" },
                            { "Vfx_Viktor_AllNotebooks6", "THE HARDEST EXAMPLE YOU'VE EVER SEEN," },
                            { "Vfx_Viktor_AllNotebooks7", "BITCH!" },
                            { "Vfx_Viktor_JacketIntro1", "NO!" },
                            { "Vfx_Viktor_JacketIntro2", "MY JACKET." },
                            { "Vfx_Viktor_JacketIntro3", "I will show you!" },
                            { "Vfx_Viktor_JacketIntro4", "Stay here, I will change it." },
                            { "Vfx_Viktor_Jacket1", "Come on, this is not funny at all." },
                            { "Vfx_Viktor_Jacket2", "Damn it." },
                            { "Vfx_Viktor_Jacket3", "Oh my god." },
                            { "Vfx_Viktor_Jacket4", "Are you joking?" },
                            { "Sfx_Viktor_Scream", "ARRGUUUAAGHHH!!!" },
                            { "Vfx_Viktor_Praise", "Good job!" },
                            { "Sfx_Viktor_Walk", "*Scraping sounds*" },
                            { "Sfx_Viktor_Notebook", "*Triple beeps*" },
                        };
                }
            });
            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), false);

            ModdedSaveGame.AddSaveHandler(Info);
        }

        IEnumerator PreLoad()
        {
            yield return 1;
            yield return "IS THAT VIKTOR STROBOVSKI?!";
            Color ViktorSubtitleColor = new Color(0.61798f, 0.4428f, 0.1943f, 1f);
            viktorAssets.AddRange([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_intro.ogg"), "Vfx_Viktor_Intro", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_youshouldnt.ogg"), "Vfx_Viktor_Triggered", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_hardmode.ogg"), "Vfx_Viktor_Hard1", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_congrats.ogg"), "Vfx_Viktor_AllNotebooks1", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_jacket0.ogg"), "Vfx_Viktor_JacketIntro1", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_jacket1.ogg"), "Vfx_Viktor_Jacket1", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_jacket2.ogg"), "Vfx_Viktor_Jacket2", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_jacket3.ogg"), "Vfx_Viktor_Jacket3", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_jacket4.ogg"), "Vfx_Viktor_Jacket4", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_scream.ogg"), "Sfx_Viktor_Scream", SoundType.Effect, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_goodjob.ogg"), "Vfx_Viktor_Praise", SoundType.Voice, ViktorSubtitleColor),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Victor_walk.ogg"), "Sfx_Viktor_Walk", SoundType.Effect, ViktorSubtitleColor),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "notebook", "viktor_math_jingle.ogg"), "Sfx_Viktor_Notebook", SoundType.Effect, Color.white),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Music", "mus_MathLevel.ogg"), "mus_MathLevel", SoundType.Music, Color.clear, 0f),
                ], [
                    "Viktor/Intro",
                    "Viktor/Triggered",
                    "Viktor/HalfNotebooks",
                    "Viktor/LastNotebook",
                    // These are stupidly funny from a Teacher who harasses the player with cruel language.
                    "Viktor/DirtyJacketFirst",
                    "Viktor/DirtyJacket1",
                    "Viktor/DirtyJacket2",
                    "Viktor/DirtyJacket3",
                    "Viktor/DirtyJacket4",
                    "Viktor/Jumpscare",
                    "Viktor/Praise",
                    "Viktor/Walk",

                    "Viktor/NotebookJingle",
                    
                    "Music/MathLevel"
                    ]);
            viktorAssets.Get<SoundObject>("Viktor/HalfNotebooks").additionalKeys = [
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_Hard2",
                    time = 4.015f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_Hard3",
                    time = 6.083f
                }
                ];
            viktorAssets.Get<SoundObject>("Viktor/LastNotebook").additionalKeys = [
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_AllNotebooks2",
                    time = 2.09f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_AllNotebooks3",
                    time = 5f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_AllNotebooks4",
                    time = 6.35f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_AllNotebooks5",
                    time = 8.52f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_AllNotebooks6",
                    time = 10.776f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_AllNotebooks7",
                    time = 13.22f
                }
                ];
            viktorAssets.Get<SoundObject>("Viktor/DirtyJacketFirst").additionalKeys = [
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_JacketIntro2",
                    time = 1.53f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_JacketIntro3",
                    time = 2.96f
                },
                new SubtitleTimedKey()
                {
                    key = "Vfx_Viktor_JacketIntro4",
                    time = 4.438f
                },
                ];
            viktorAssets.Add("PrincipalPoster", AssetLoader.TextureFromMod(this, "Texture2D", "PRI_Viktor.png"));
            viktorAssets.Add("Notebook", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 30f, "notebook", "viktor_math.png"));
            viktorAssets.Add("ViktorSubsitute", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 16f, "Texture2D", "Viktor_Subsitute.png"));
            viktorAssets.Add("ViktorEvil", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 16f, "Texture2D", "Viktor_Evil.png"));

            Viktor = new NPCBuilder<Viktor>(Info)
                .SetName("Viktor Strobovski")
                .SetEnum("ViktorStrobovski")
                .SetPoster(viktorAssets.Get<Texture2D>("PrincipalPoster"), "PRI_ViktorStrobovski1", "PRI_ViktorStrobovski2")
                .AddLooker()
                .AddTrigger()
                .IgnorePlayerOnSpawn()
                .DisableNavigationPrecision()
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetMetaTags(["teacher"])
                .Build();
            Viktor.audMan = Viktor.GetComponent<AudioManager>();
            Viktor.audMan.ReflectionSetVariable("subtitleColor", ViktorSubtitleColor);
            Viktor.audMan.ReflectionSetVariable("overrideSubtitleColor", true);
            Viktor.Navigator.SetRoomAvoidance(false);
            Viktor.spriteRenderer[0].sprite = viktorAssets.Get<Sprite>("ViktorSubsitute");

            GeneratorManagement.Register(this, GenerationModType.Addend, EditGenerator);
        }

        // I had to rewrite the source code without the base, but it's still her own port.
        private void EditGenerator(string floorName, int floorNumber, SceneObject floorObject)
        {
            // It is good practice to check if the level starts with F to make sure to not clash with other mods.
            // INF stands for Infinite Floor
            if (floorName.StartsWith("F") || floorName.StartsWith("END") || floorName.Equals("INF"))
            {
                foreach (var ld in floorObject.GetCustomLevelObjects())
                {
                    ld.AddPotentialTeacher(Viktor, ViktorConfiguration.Weight.Value);
                    ld.AddPotentialAssistingTeacher(Viktor, ViktorConfiguration.Weight.Value);
                }
                print($"Added Viktor Strobovski to {floorName} (Floor {floorNumber})");
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.teacherextension.viktor";
        public const string PLUGIN_NAME = "Viktor Strobovski TeacherAPI Port (Continued)";
        public const string PLUGIN_VERSION = "1.0.0.0";
    }
}
