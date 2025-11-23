using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeacherAPI;
using UnityEngine;

namespace TeacherExtension.Baldimore;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("alexbw145.baldiplus.teacherapi", "0.1.6")]
[BepInDependency("pixelguy.pixelmodding.baldiplus.balditvannouncer", BepInDependency.DependencyFlags.SoftDependency)]
public class BaldiPlugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "alexbw145.baldiplus.teacherextension.baldi";
    private const string PLUGIN_NAME = "Baldi TeacherAPI Port";
    private const string PLUGIN_VERSION = "1.4";
    internal static ConfigEntry<int> BaldiWeight;
    internal static ConfigEntry<bool> EveryAssistantIsHere;

    private void Awake()
    {
        Harmony harmony = new Harmony(PLUGIN_GUID);
        BaldiWeight = Config.Bind(
                "Baldi",
                "Weight",
                99,
                "The higher the weight number, the more there is a chance of him spawning. (Defaults to 99. For comparison, base game Baldi weight is 100) (Requires Restart)"
            );
        EveryAssistantIsHere = Config.Bind(
                "Baldi",
                "All Assistants Spawn",
                false,
                "If the TeacherAPI port of Baldi is chosen as the main teacher, he'll grab every possible assistants to be put into one level. (Not guaranteed for winning & sometimes teachers will have 0 notebooks assigned leading to possible exclusion)"
            );
        harmony.PatchAllConditionals();
        LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), LoadingEventOrder.Pre);

        ModdedSaveGame.AddSaveHandler(Info);
        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.balditvannouncer"))
            AnnouncerTVFixer.UnpatchBaldiTVAnnouncements(harmony);

    }

    IEnumerator PreLoad()
    {
        yield return 1;
        yield return "Welcome to Baldi's Basics!";
        var baldiMeta = NPCMetaStorage.Instance.Get(Character.Baldi);
        var theBald = baldiMeta.prefabs.First().Value as Baldi;
        var baldi = new NPCBuilder<TeacherBaldi>(Info)
            .SetName("Baldi_TeacherAPI")
            .SetEnum(Character.Baldi)
            .AddLooker()
            .AddTrigger()
            .AddSpawnableRoomCategories(RoomCategory.Null)
            .SetWanderEnterRooms()
            .SetPoster(theBald.Poster)
            .SetMinMaxAudioDistance(10f, 300f)
            .SetForcedSubtitleColor((Color)theBald.gameObject.GetComponent<AudioManager>().ReflectionGetVariable("subtitleColor"))
            .SetMetaTags(["teacher", "faculty"])
            .Build();
        TeacherPlugin.RegisterTeacher(baldi);
        baldi.Navigator.maxSpeed = 0f;
        baldi.Navigator.speed = 0f;
        baldi.Navigator.accel = 0f;
        baldi.audMan = baldi.gameObject.GetComponent<AudioManager>();
        baldi.animator = baldi.gameObject.AddComponent<Animator>();
        baldi.Navigator.passableObstacles.Add(PassableObstacle.LockedDoor);
        baldi.audCountdown = Resources.FindObjectsOfTypeAll<HappyBaldi>().Last().ReflectionGetVariable("audCountdown") as SoundObject[];
        baldi.audHere = Resources.FindObjectsOfTypeAll<HappyBaldi>().Last().ReflectionGetVariable("audHere") as SoundObject;
        baldi.animator.runtimeAnimatorController = Resources.FindObjectsOfTypeAll<HappyBaldi>().Last().gameObject.GetComponent<Animator>().runtimeAnimatorController;
        baldi.spoopAnimController = theBald.gameObject.GetComponent<Animator>().runtimeAnimatorController;
        baldi.animator.updateMode = AnimatorUpdateMode.Normal;
        baldi.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        baldi.volumeAnimator = baldi.gameObject.AddComponent<VolumeAnimator>();
        baldi.volumeAnimator.enabled = false;
        baldi.volumeAnimator.ReflectionSetVariable("animator", baldi.animator as Animator);
        baldi.volumeAnimator.ReflectionSetVariable("fallbackAudioManager", baldi.AudMan as AudioManager);
        baldi.volumeAnimator.ReflectionSetVariable("audioSource", baldi.AudMan.audioDevice as AudioSource);
        baldi.volumeAnimator.sensitivity = ((VolumeAnimator)TeacherBaldi._volumeAnimator.GetValue(theBald)).sensitivity;
        baldi.volumeAnimator.animationName = "BAL_Smile";
        baldi.volumeAnimator.bufferTime = 0.1f;
        //Baldi.slap = theBald.ReflectionGetVariable("slap") as SoundObject;
        baldi.slap = AccessTools.Field(typeof(Baldi), "slap").GetValue(theBald) as SoundObject;
        //Baldi.rulerBreak = theBald.ReflectionGetVariable("rulerBreak") as SoundObject;
        baldi.rulerBreak = AccessTools.Field(typeof(Baldi), "rulerBreak").GetValue(theBald) as SoundObject;
        baldi.ReflectionSetVariable("audAppleThanks", theBald.ReflectionGetVariable("audAppleThanks") as SoundObject);
        baldi.correctSounds = theBald.ReflectionGetVariable("correctSounds") as WeightedSoundObject[];
        baldi.loseSounds = theBald.loseSounds;
        baldi.ReflectionSetVariable("eatSounds", theBald.ReflectionGetVariable("eatSounds") as WeightedSoundObject[]);
        List<Sprite> manual = new List<Sprite>();
        for (int i = 0; i <= 99; i++)
        {
            string uh = i >= 10 ? "00" : "000";
            Sprite itexists = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(spr => spr.name == $"Baldi_Wave{uh}{i}");
            if (itexists != null)
                manual.Add(itexists);
        }
        baldi.animatorForIntro = baldi.gameObject.AddComponent<CustomSpriteRendererAnimator>();
        baldi.animatorForIntro.renderer = baldi.spriteRenderer[0];
        baldi.animatorForIntro.useScaledTime = true;
        baldi.animatorForIntro.timeScale = TimeScaleType.Npc;
        baldi.animatorForIntro.AddAnimation("Wavee", new SpriteAnimation(manual.ToArray(), 1.683f));
        baldi.count = Resources.FindObjectsOfTypeAll<Sprite>().Last(spr => spr.name == "BAL_Countdown_Sheet_1");
        baldi.countpeek = Resources.FindObjectsOfTypeAll<Sprite>().Last(spr => spr.name == "BAL_Countdown_Sheet_2");
        baldi.countidle = Resources.FindObjectsOfTypeAll<Sprite>().Last(spr => spr.name == "BAL_Countdown_Sheet_0");
        baldi.introSpr = manual[0];

        GeneratorManagement.Register(this, GenerationModType.Addend, (title, num, scene) =>
        {
            var meta = scene.GetMeta();
            if (meta == null) return;
            foreach (var levelObject in scene.GetCustomLevelObjects())
            {
                if (levelObject.IsModifiedByMod(Info)) continue;
                levelObject.AddPotentialTeacher(baldi, BaldiWeight.Value);
                levelObject.AddPotentialAssistingTeacher(baldi, BaldiWeight.Value);
                levelObject.MarkAsModifiedByMod(Info);
            }
        });
    }
}
