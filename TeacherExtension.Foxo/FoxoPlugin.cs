using BepInEx;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Linq;
using TeacherAPI;
using TeacherExtension.Foxo.Items;
using UnityCipher;
using UnityEngine;
using static BepInEx.BepInDependency;

namespace TeacherExtension.Foxo
{
    [BepInPlugin("alexbw145.baldiplus.teacherextension.foxo", "Foxo Teacher for MoreTeachers", "1.1.0.0")]
    [BepInDependency("alexbw145.baldiplus.teacherapi", DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", DependencyFlags.HardDependency)]
    public class FoxoPlugin : BaseUnityPlugin
    {
        public static FoxoPlugin Instance { get; private set; }
        public Foxo Foxo { get; private set; }
        public Foxo DarkFoxo { get; private set; }
        public FoxoSave deathCounter = new FoxoSave();
        public static AssetManager ItemAssets = new AssetManager();

        internal void Awake()
        {
            new Harmony("alexbw145.baldiplus.teacherextension.foxo").PatchAllConditionals();
            Instance = this;
            FoxoConfiguration.Setup();
            AssetLoader.LocalizationFromMod(this);
            TeacherPlugin.RequiresAssetsFolder(this); // Critical!!!
            LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoaded, LoadingEventOrder.Pre);
            ModdedSaveGame.AddSaveHandler(deathCounter);
        }

        private Foxo NewFoxo(string name)
        {
            var newFoxo = new NPCBuilder<Foxo>(Info)
                .SetName(name)
                .SetEnum(name)
                .SetPoster(ObjectCreators.CreatePosterObject(new Texture2D[] { AssetLoader.TextureFromMod(this, "poster.png") }))
                .AddLooker()
                .AddTrigger()
                .DisableNavigationPrecision()
                .SetMetaTags(new string[] { "teacher", "faculty" })
                .Build();
            newFoxo.ReflectionSetVariable("audMan", newFoxo.GetComponent<AudioManager>());
            newFoxo.Navigator.passableObstacles.Add(PassableObstacle.LockedDoor);

            // Adds a custom animator
            CustomSpriteAnimator animator = newFoxo.gameObject.AddComponent<CustomSpriteAnimator>();
            animator.spriteRenderer = newFoxo.spriteRenderer[0];
            newFoxo.animator = animator;
            return newFoxo;
        }

        private void OnAssetsLoaded()
        {
            // I'm dead serious when they always get unloaded!
            Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name.ToLower() == "exit_transparent").MarkAsNeverUnload();
            Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name.ToLower() == "exit").MarkAsNeverUnload();
            Foxo.LoadAssets();

            // Create and Register Foxo and DarkFoxo
            {
                Foxo = NewFoxo("Foxo");
                DarkFoxo = NewFoxo("WrathFoxo");
                DarkFoxo.forceWrath = true;

                TeacherPlugin.RegisterTeacher(Foxo);
                TeacherPlugin.RegisterTeacher(DarkFoxo);
            }
            // Also create and register some items specifically to combat against Foxo.
            {
                var fireExtinguish = new ItemBuilder(Info)
                    .SetNameAndDescription("Itm_FireExtinguisher", "Desc_FireExtinguisher")
                    .SetItemComponent<FireExtinguisher>()
                    .SetEnum(global::Items.Apple)
                    .SetGeneratorCost(ItemMetaStorage.Instance.FindByEnum(global::Items.Apple).value.value)
                    .SetShopPrice(ItemMetaStorage.Instance.FindByEnum(global::Items.Apple).value.price)
                    .SetSprites(Foxo.sprites.Get<Sprite>("Items/FireExtinguisher_Small"), Foxo.sprites.Get<Sprite>("Items/FireExtinguisher_Large"))
                    .SetMeta(ItemFlags.Persists, new string[] { "alternative" })
                    .Build();
                ItemAssets.Add("FireExtinguisher", fireExtinguish);
            }

            GeneratorManagement.Register(this, GenerationModType.Addend, EditGenerator);
        }

        private void EditGenerator(string floorName, int floorNumber, SceneObject floorObject)
        {
            // It is good practice to check if the level starts with F to make sure to not clash with other mods.
            // INF stands for Infinite Floor
            if (floorName.StartsWith("F") || floorName.StartsWith("END") || floorName.Equals("INF"))
            {
                foreach (var ld in floorObject.GetCustomLevelObjects())
                {
                    ld.AddPotentialTeacher(Foxo, FoxoConfiguration.Weight.Value);
                    ld.AddPotentialAssistingTeacher(Foxo, FoxoConfiguration.Weight.Value);
                }
                print($"Added Foxo to {floorName} (Floor {floorNumber})");
            }
        }
    }

    public class FoxoSave : ModdedSaveGameIOText
    {
        public override BepInEx.PluginInfo pluginInfo => FoxoPlugin.Instance.Info;
        public int deaths { get; internal set; }

        public override void LoadText(string toLoad)
        {
            deaths = Convert.ToInt32(RijndaelEncryption.Decrypt(toLoad, "FoxoTeacher_" + PlayerFileManager.Instance.fileName));
        }

        public override void Reset()
        {
            deaths = 0;
        }

        public override string SaveText()
        {
            return RijndaelEncryption.Encrypt(deaths.ToString(), "FoxoTeacher_" + PlayerFileManager.Instance.fileName);
        }

        public override void OnCGMCreated(CoreGameManager instance, bool isFromSavedGame)
        {
            if (!isFromSavedGame)
                deaths = 0;
        }
    }
}
