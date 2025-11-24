using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using MTM101BaldAPI.UI;
using System;
using System.Collections;
using System.Linq;
using TeacherAPI;
using TeacherExtension.Foxo;
using TeacherExtension.Foxo.Items;
using UnityCipher;
using UnityEngine;
using UnityEngine.UI;

namespace TeacherExtension.Foxo
{
    [BepInPlugin("alexbw145.baldiplus.teacherextension.foxo", "Foxo Teacher for MoreTeachers", "1.1.0.1")]
    [BepInDependency("alexbw145.baldiplus.teacherapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    public class FoxoPlugin : BaseUnityPlugin
    {
        public static FoxoPlugin Instance { get; private set; }
        public Foxo foxo { get; private set; }
        public Foxo darkFoxo { get; private set; }
        public FoxoSave deathCounter = new FoxoSave();
        internal static PassableObstacle waterbucketofwaterPassable;

        internal void Awake()
        {
            new Harmony("alexbw145.baldiplus.teacherextension.foxo").PatchAllConditionals();
            Instance = this;
            FoxoConfiguration.Setup();
            AssetLoader.LocalizationFromMod(this);
            TeacherPlugin.RequiresAssetsFolder(this); // Critical!!!
            waterbucketofwaterPassable = EnumExtensions.ExtendEnum<PassableObstacle>("WaterBucketOfWater");
            LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoaded, LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                foreach (var npc in NPCMetaStorage.Instance.FindAll(x => x.value.GetType().Equals(typeof(ArtsAndCrafters)) || x.value.GetType().Equals(typeof(GottaSweep)) || x.value.Character == Character.DrReflex || x.character.ToStringExtended() == "ViktorStrobovski"
                || x.tags.Contains("foxoteacherapi_hateswater")))
                {
                    foreach (var prefab in npc.prefabs)
                        prefab.Value.Navigator.passableObstacles.Add(waterbucketofwaterPassable);
                }
            }, LoadingEventOrder.Final);
            ModdedSaveGame.AddSaveHandler(deathCounter);
        }

        private Foxo NewFoxo(string name)
        {
            var newFoxo = new NPCBuilder<Foxo>(Info)
                .SetName(name)
                .SetEnum(name)
                .SetPoster(Foxo.foxoAssets.Get<Texture2D>("PosterBase"), "PRI_Foxo1", "PRI_Foxo2")
                .AddLooker()
                .AddTrigger()
                .DisableNavigationPrecision()
                .SetWanderEnterRooms()
                .SetMetaTags(new string[] { "teacher", "faculty" })
                .Build();
            newFoxo.Navigator.accel = 0f;
            newFoxo.audMan = newFoxo.GetComponent<AudioManager>();
            newFoxo.Navigator.passableObstacles.AddRange(new PassableObstacle[] { PassableObstacle.LockedDoor, waterbucketofwaterPassable });
            newFoxo.correctSounds = Foxo.foxoAssets.Get<WeightedSoundObject[]>("praise");

            // Adds a custom animator
            CustomSpriteRendererAnimator animator = newFoxo.gameObject.AddComponent<CustomSpriteRendererAnimator>();
            animator.renderer = newFoxo.spriteRenderer[0];
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
                foxo = NewFoxo("Foxo");
                darkFoxo = NewFoxo("WrathFoxo");
                var meta = foxo.GetMeta();
                if (meta.value != foxo) // Playable Chars FDLC1 conflict begone.
                    meta.ReflectionSetVariable("defaultKey", foxo.name);
                darkFoxo.forceWrath = true;
                foxo.slap = Foxo.foxoAssets.Get<SoundObject>("slap");
                darkFoxo.slap = Foxo.foxoAssets.Get<SoundObject>("slap2");

                var waveSprites = Foxo.foxoAssets.Get<Sprite[]>("Wave");
                var slapSprites = Foxo.foxoAssets.Get<Sprite[]>("Slap");
                var wrathSprites = Foxo.foxoAssets.Get<Sprite[]>("Wrath");
                foxo.animator.AddAnimation("Wave", new SpriteAnimation(waveSprites, 3f));
                foxo.animator.AddAnimation("Happy", new SpriteAnimation(new Sprite[] { waveSprites[waveSprites.Length - 1] }, 1f));

                foxo.animator.AddAnimation("Slap", new SpriteAnimation(slapSprites, 1f));
                foxo.animator.AddAnimation("SlapIdle", new SpriteAnimation(new Sprite[] { slapSprites[slapSprites.Length - 1] }, 1f));
                foxo.animator.AddAnimation("Sprayed", new SpriteAnimation(Foxo.foxoAssets.Get<Sprite[]>("Sprayed"), 0.1f));
                foxo.animator.AddAnimation("Jump", new SpriteAnimation(Foxo.foxoAssets.Get<Sprite[]>("Jump"), 0.2f));
                //animator.animations.Add("JumpIdle", new CustomAnimation<Sprite>(new Sprite[] { Foxo.sprites.Get<Sprite[]>("Jump").Last() }, 1f));

                foxo.animator.AddAnimation("WrathIdle", new SpriteAnimation(new Sprite[] { wrathSprites[0] }, 1f));
                foxo.animator.AddAnimation("Wrath", new SpriteAnimation(wrathSprites.Reverse().ToArray(), 0.3f));
                foxo.animator.AddAnimation("WrathSprayed", new SpriteAnimation(Foxo.foxoAssets.Get<Sprite[]>("WrathSprayed"), 0.02f));

                TeacherPlugin.RegisterTeacher(foxo);
                TeacherPlugin.RegisterTeacher(darkFoxo);
                foxo.AddNewBaldiInteraction<HideableLockerBaldiInteraction>(
                check: CustomFoxoInteractions.LockerCheck,
                trigger: CustomFoxoInteractions.LockerInteract,
                payload: CustomFoxoInteractions.LockerPayload);
                BaldiTVExtensionHandler.AddCharacter("Foxo", new FoxoWrathTV());
            }
            // Also create and register some items specifically to combat against Foxo.
            {
                var appleMeta = ItemMetaStorage.Instance.FindByEnum(global::Items.Apple);
                var fireExtinguish = new ItemBuilder(Info)
                    .SetNameAndDescription("Itm_FireExtinguisher", "Desc_FireExtinguisher")
                    .SetItemComponent<FireExtinguisher>()
                    .SetEnum("FireExtinguisher")
                    .SetGeneratorCost(appleMeta.value.value)
                    .SetShopPrice(appleMeta.value.price)
                    .SetSprites(Foxo.foxoAssets.Get<Sprite>("Items/FireExtinguisher_Small"), Foxo.foxoAssets.Get<Sprite>("Items/FireExtinguisher_Large"))
                    .SetMeta(ItemFlags.Persists, new string[] { "alternative" })
                    .Build();
                fireExtinguish.itemType = global::Items.Apple;
                var appleItems = appleMeta.itemObjects.ToList();
                appleItems.Insert(0, fireExtinguish);
                appleMeta.itemObjects = appleItems.ToArray();
                Foxo.foxoAssets.Add("FireExtinguisher", fireExtinguish);
            }
            {
                var bucketofwater = new ItemBuilder(Info)
                    .SetNameAndDescription("Itm_WaterBucketOfWater", "Desc_WaterBucketOfWater")
                    .SetItemComponent<WaterBucketOfWater>()
                    .SetEnum("WaterBucketOfWater")
                    .SetGeneratorCost(ItemMetaStorage.Instance.FindByEnum(global::Items.Wd40).value.value + 30)
                    .SetShopPrice(ItemMetaStorage.Instance.FindByEnum(global::Items.Wd40).value.price)
                    .SetSprites(Foxo.foxoAssets.Get<Sprite>("Items/WaterBucketOfWater_Small"), Foxo.foxoAssets.Get<Sprite>("Items/WaterBucketOfWater_Large"))
                    .SetMeta(ItemFlags.Persists, new string[] { "alternative", "crmp_contraband" }) // Too much liquids contained, must be contraband.
                    .Build();
                GameObject quad = Instantiate(Resources.FindObjectsOfTypeAll<Chalkboard>().ToList().First(), bucketofwater.item.transform, false).gameObject;
                quad.transform.Find("Chalkbaord").Find("Quad").SetParent(bucketofwater.item.transform, false);
                Destroy(quad.gameObject);
                bucketofwater.item.transform.Find("Quad").rotation = Quaternion.Euler(90f, 0f, 0f);
                bucketofwater.item.transform.Find("Quad").localPosition = new Vector3(0f, -4.9f, 0f);
                bucketofwater.item.transform.Find("Quad").localScale = new Vector3(10f, 10f, 10f);
                Material spillmat = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "BlankChalk"));
                spillmat.name = "WaterBucketOfWaterSpill";
                spillmat.SetMainTexture(AssetLoader.TextureFromMod(this, "items", "WaterBucketWater_Splash.png"));
                bucketofwater.item.transform.Find("Quad").GetComponent<MeshRenderer>().SetMaterial(spillmat);
                bucketofwater.item.gameObject.GetComponent<WaterBucketOfWater>().wrongPlacement = Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "ErrorMaybe");
                bucketofwater.item.gameObject.GetComponent<WaterBucketOfWater>().spill = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "audio", "WaterSplash.wav"), "Sfx_WaterBucketOfWater_Spill", SoundType.Effect, Color.white, 1f);
                Foxo.foxoAssets.Add("WaterBucketOfWater", bucketofwater);
            }

            GeneratorManagement.Register(this, GenerationModType.Addend, EditGenerator);
        }

        private void EditGenerator(string floorName, int floorNumber, SceneObject floorObject)
        {
            var meta = floorObject.GetMeta();
            if (meta == null) return; // Tried checking the tags, I think I checked the wrong way.
            bool flag = false;
            foreach (var ld in floorObject.GetCustomLevelObjects())
            {
                if (ld.IsModifiedByMod(Info)) continue;
                ld.AddPotentialTeacher(foxo, FoxoConfiguration.Weight.Value);
                ld.AddPotentialAssistingTeacher(foxo, FoxoConfiguration.Weight.Value);
                if (floorNumber >= 2)
                    ld.potentialItems = ld.potentialItems.AddToArray(new WeightedItemObject()
                    {
                        selection = Foxo.foxoAssets.Get<ItemObject>("FireExtinguisher"),
                        weight = 20
                    });
                ld.potentialItems = ld.potentialItems.AddToArray(new WeightedItemObject()
                {
                    selection = Foxo.foxoAssets.Get<ItemObject>("WaterBucketOfWater"),
                    weight = 35
                });
                ld.MarkAsModifiedByMod(Info);
                flag = true;
            }
            if (flag)
                print($"Added Foxo to {floorName} (Floor {floorNumber})");
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

internal class FoxoWrathTV : BaldiTVCharacter
{
    public override bool SoundBelongsToCharacter(SoundObject obj) => obj == Foxo.foxoAssets.Get<SoundObject>("WrathEventAud");

    public override IEnumerator SpeakEnumerator(Image image, BaldiTV tv, AudioManager audMan, SoundObject sound)
    {
        var foxoSprites = Foxo.foxoAssets.GetAll<Sprite[]>().ToList().FindAll(x => x.ToList().Find(f => !f.name.ToLower().Contains("wrath") && !f.name.ToLower().Contains("notebook")));
        audMan.QueueAudio(sound);
        yield return null;
        while (audMan.QueuedAudioIsPlaying)
        {
            image.sprite = foxoSprites[UnityEngine.Random.RandomRangeInt(0, foxoSprites.Count - 1)].First();
            yield return null;
        }
        yield break;
    }
}