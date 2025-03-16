using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections;
using System.Linq;
using UnityEngine;
using TeacherAPI;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace TeacherExtension.Baldimore;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("alexbw145.baldiplus.teacherapi", "0.1.5")]
public class BaldiPlugin : BaseUnityPlugin
{
    internal static ConfigEntry<int> BaldiWeight;

    private void Awake()
    {
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        BaldiWeight = Config.Bind(
                "Baldi",
                "Weight",
                99,
                "The higher the weight number, the more there is a chance of him spawning. (Defaults to 99. For comparison, base game Baldi weight is 100) (Requires Restart)"
            );
        harmony.PatchAllConditionals();
        LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), false);

        ModdedSaveGame.AddSaveHandler(Info);
    }

    IEnumerator PreLoad()
    {
        yield return 1;
        yield return "Welcome to Baldi's Basics!";
        var baldiMeta = NPCMetaStorage.Instance.Get(Character.Baldi);
        var theBald = baldiMeta.prefabs.First().Value as Baldi;
        var Baldi = new NPCBuilder<TeacherBaldi>(Info)
            .SetName("Baldi_TeacherAPI")
            .SetEnum(Character.Baldi)
            .DisableNavigationPrecision()
            .AddLooker()
            .AddTrigger()
            .AddSpawnableRoomCategories(RoomCategory.Null)
            .SetPoster(theBald.Poster)
            .SetMinMaxAudioDistance(10f, 300f)
            .SetForcedSubtitleColor((Color)theBald.gameObject.GetComponent<AudioManager>().ReflectionGetVariable("subtitleColor"))
            .SetMetaTags(["teacher"])
            .Build();
        Baldi.audMan = Baldi.gameObject.GetComponent<AudioManager>();
        Baldi.animator = Baldi.gameObject.AddComponent<Animator>();
        Baldi.audCountdown = Resources.FindObjectsOfTypeAll<HappyBaldi>().Last().ReflectionGetVariable("audCountdown") as SoundObject[];
        Baldi.audHere = Resources.FindObjectsOfTypeAll<HappyBaldi>().Last().ReflectionGetVariable("audHere") as SoundObject;
        Baldi.animator.runtimeAnimatorController = Resources.FindObjectsOfTypeAll<HappyBaldi>().Last().gameObject.GetComponent<Animator>().runtimeAnimatorController;
        Baldi.spoopAnimController = theBald.gameObject.GetComponent<Animator>().runtimeAnimatorController;
        Baldi.animator.updateMode = AnimatorUpdateMode.Normal;
        Baldi.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Baldi.volumeAnimator = Baldi.gameObject.AddComponent<VolumeAnimator>();
        Baldi.volumeAnimator.ReflectionSetVariable("animator", Baldi.animator as Animator);
        Baldi.volumeAnimator.ReflectionSetVariable("fallbackAudioManager", Baldi.audMan as AudioManager);
        Baldi.volumeAnimator.ReflectionSetVariable("audioSource", Baldi.audMan.audioDevice as AudioSource);
        //Baldi.slap = theBald.ReflectionGetVariable("slap") as SoundObject;
        AccessTools.Field(typeof(Baldi), "slap").SetValue(Baldi, theBald.ReflectionGetVariable("slap") as SoundObject);
        //Baldi.rulerBreak = theBald.ReflectionGetVariable("rulerBreak") as SoundObject;
        AccessTools.Field(typeof(Baldi), "rulerBreak").SetValue(Baldi, theBald.ReflectionGetVariable("rulerBreak") as SoundObject);
        Baldi.ReflectionSetVariable("audAppleThanks", theBald.ReflectionGetVariable("audAppleThanks") as SoundObject);
        Baldi.ReflectionSetVariable("audAppleThanks", theBald.ReflectionGetVariable("audAppleThanks") as SoundObject);
        Baldi.ReflectionSetVariable("correctSounds", theBald.ReflectionGetVariable("correctSounds") as WeightedSoundObject[]);
        Baldi.loseSounds = theBald.loseSounds;
        Baldi.ReflectionSetVariable("eatSounds", theBald.ReflectionGetVariable("eatSounds") as WeightedSoundObject[]);
        AccessTools.Field(typeof(Baldi), "animator").SetValue(Baldi, Baldi.animator as Animator);
        AccessTools.Field(typeof(Baldi), "volumeAnimator").SetValue(Baldi, Baldi.volumeAnimator as VolumeAnimator);
        AccessTools.Field(typeof(Baldi), "audMan").SetValue(Baldi, Baldi.audMan as AudioManager);
        List<Sprite> manual = new List<Sprite>();
        for (int i = 0; i <= 99; i++)
        {
            string uh = i >= 10 ? "00" : "000";
            Sprite itexists = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(spr => spr.name == $"Baldi_Wave{uh}{i}");
            if (itexists != null)
                manual.Add(itexists);
        }
        Baldi.spritesofIntro = manual.ToArray();
        Baldi.count = Resources.FindObjectsOfTypeAll<Sprite>().Last(spr => spr.name == "BAL_Countdown_Sheet_1");
        Baldi.countpeek = Resources.FindObjectsOfTypeAll<Sprite>().Last(spr => spr.name == "BAL_Countdown_Sheet_2");
        Baldi.countidle = Resources.FindObjectsOfTypeAll<Sprite>().Last(spr => spr.name == "BAL_Countdown_Sheet_0");

        GeneratorManagement.Register(this, GenerationModType.Addend, (title, num, scene) =>
        {
            scene?.CustomLevelObject()?.AddPotentialTeacher(Baldi, BaldiWeight.Value);
            scene?.CustomLevelObject()?.AddPotentialAssistingTeacher(Baldi, BaldiWeight.Value);
        });
    }
}

public static class PluginInfo
{
    public const string PLUGIN_GUID = "alexbw145.baldiplus.teacherextension.baldi";
    public const string PLUGIN_NAME = "Baldi TeacherAPI Port";
    public const string PLUGIN_VERSION = "1.0";
}
