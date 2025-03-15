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

namespace TeacherExtension.Baldimore;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("alexbw145.baldiplus.teacherapi", "0.1.5")]
public class BaldiPlugin : BaseUnityPlugin
{
    public static BaldiPlugin Instance { get; private set; }

    private void Awake()
    {
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        Instance = this;
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

        GeneratorManagement.Register(this, GenerationModType.Addend, (title, num, scene) =>
        {
            scene?.CustomLevelObject()?.AddPotentialTeacher(Baldi, 99);
            scene?.CustomLevelObject()?.AddPotentialAssistingTeacher(Baldi, 99);
        });
    }
}

public static class PluginInfo
{
    public const string PLUGIN_GUID = "alexbw145.baldiplus.teacherextension.baldi";
    public const string PLUGIN_NAME = "Baldi TeacherAPI Port";
    public const string PLUGIN_VERSION = "0.0.0.0";
}
