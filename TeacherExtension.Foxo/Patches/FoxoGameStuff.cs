using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using TeacherAPI;
using TeacherExtension.Foxo.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TeacherExtension.Foxo.Patches
{
    [HarmonyPatch]
    class GameManStuff
    {
        [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.EndGame)), HarmonyPostfix]
        static void DeathCounterIncrease() => FoxoPlugin.Instance.deathCounter.deaths++;

        /*[HarmonyPatch(typeof(Navigator), "TempOpenObstacles"), HarmonyPostfix]
        static void TempOpenInaccessible(Navigator __instance)
        {
            if (!__instance.passableObstacles.Contains(FoxoPlugin.foxoUnpassable))
                Foxo.tempOpenSpecial?.Invoke();
        }
        [HarmonyPatch(typeof(Navigator), "TempCloseObstacles"), HarmonyPostfix]
        static void TempCloseInaccessible(Navigator __instance)
        {
            if (__instance.passableObstacles.Contains(FoxoPlugin.foxoUnpassable))
                Foxo.tempCloseSpecial?.Invoke();
        }*/

        static bool foxoinF3 = false;
        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.LoadNextLevel)), HarmonyPrefix]
        static void IsFoxoF3Teach() => foxoinF3 = !TeacherPlugin.IsEndlessFloorsLoaded() && TeacherManager.Instance != null && TeacherManager.Instance?.SpawnedMainTeacher != null && TeacherManager.Instance?.SpawnedMainTeacher?.GetComponent<Foxo>() != null;

        [HarmonyPatch(typeof(PlaceholderWinManager), nameof(PlaceholderWinManager.Initialize)), HarmonyPrefix]
        static void FoxoEnding(PlaceholderWinManager __instance)
        {
            if (!foxoinF3) return;
            {
                VideoPlayer video = __instance.gameObject.AddComponent<VideoPlayer>();
                video.playOnAwake = false;
                video.skipOnDrop = false;
                video.renderMode = VideoRenderMode.CameraNearPlane;
                video.targetCamera = CoreGameManager.Instance.GetCamera(0).camCom;
                CoreGameManager.Instance.GetHud(0).Hide(true);
                video.targetCameraAlpha = 1f;
                video.audioOutputMode = VideoAudioOutputMode.AudioSource;
                video.SetTargetAudioSource(0, CoreGameManager.Instance.audMan.audioDevice);
                video.waitForFirstFrame = false;
                video.source = VideoSource.Url;
                video.url = FoxoPlugin.Instance.deathCounter.deaths >= 6
                    ? Path.Combine("File:///", AssetLoader.GetModPath(FoxoPlugin.Instance), "endings", "GradeCutscene.mov")
                    : Path.Combine("File:///", AssetLoader.GetModPath(FoxoPlugin.Instance), "endings", "GradeCutscene_Good.mov");
                video.loopPointReached += (vp) => { if (FoxoPlugin.Instance.deathCounter.deaths >= 4) Application.Quit(); else Congrats(__instance); };
                video.aspectRatio = VideoAspectRatio.FitInside;
                __instance.gameObject.GetComponent<VideoPlayer>().Play(); // This is stupid...
                __instance.gameObject.GetComponent<VideoPlayer>().Pause(); // A stupid workaround...
            }
        }

        static void Congrats(PlaceholderWinManager __instance)
        {
            Canvas canv = new GameObject("Congrats", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canv.gameObject.layer = LayerMask.NameToLayer("UI");
            canv.transform.localPosition = Vector3.zero;
            canv.renderMode = RenderMode.ScreenSpaceCamera;
            canv.worldCamera = GlobalCam.Instance.Cam;
            CanvasScaler scale = canv.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(480f, 360f);
            scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scale.referencePixelsPerUnit = 100f;
            GraphicRaycaster graphic = canv.GetComponent<GraphicRaycaster>();
            graphic.ignoreReversedGraphics = true;
            graphic.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            graphic.blockingMask = ~0;
            canv.planeDistance = 0.31f;
            canv.gameObject.SetActive(false);
            Image image = new GameObject("Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(canv.transform);
            image.gameObject.layer = LayerMask.NameToLayer("UI");
            image.sprite = Foxo.sprites.Get<Sprite>("Graduated");
            image.material = Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "UI_AsSprite");
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.pivot = Vector2.one / 2f;
            image.rectTransform.sizeDelta = new Vector2(480f, 360f) / 12f;
            image.rectTransform.localPosition = Vector3.zero;
            image.rectTransform.localScale = new Vector3(0.7f, 1f, 1f);
            TextMeshProUGUI text = new GameObject("YourName", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(canv.transform);
            text.gameObject.layer = LayerMask.NameToLayer("UI");
            text.text = PlayerFileManager.Instance.fileName;
            text.font = Foxo.fonts.Get<TMP_FontAsset>("Cooper24");
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.richText = false;
            text.color = Color.black;
            text.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            text.rectTransform.pivot = new Vector2(1f, 0.5f);
            text.rectTransform.localPosition = Vector3.zero;
            text.rectTransform.anchoredPosition = new Vector2(-140f, 20f);
            Image exit = new GameObject("Exit", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            exit.rectTransform.SetParent(canv.transform);
            exit.gameObject.layer = LayerMask.NameToLayer("UI");
            exit.tag = "Button";
            exit.sprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name.ToLower().EndsWith("exit_transparent")); // For some reason, the exit sprites are unloaded or Advanced Edition messes with them.
            //exit.material = Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "UI_AsSprite");
            exit.rectTransform.anchorMin = Vector2.zero;
            exit.rectTransform.anchorMax = Vector2.zero;
            exit.rectTransform.pivot = Vector2.zero;
            exit.raycastTarget = true;
            exit.rectTransform.localPosition = Vector3.zero;
            exit.rectTransform.anchoredPosition = Vector2.right * 90f;
            exit.rectTransform.sizeDelta = Vector2.one * 50f;
            StandardMenuButton but = exit.gameObject.AddComponent<StandardMenuButton>();
            but.image = exit;
            but.unhighlightedSprite = exit.sprite;
            but.highlightedSprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name.ToLower().EndsWith("exit"));
            but.swapOnHigh = true;
            but.OnPress = new UnityEngine.Events.UnityEvent();
            but.OnPress.AddListener(() => CoreGameManager.Instance.ReturnToMenu());
            CursorInitiator initat = canv.gameObject.AddComponent<CursorInitiator>();
            initat.cursorPre = Resources.FindObjectsOfTypeAll<CursorController>().ToList().Find(x => x.name == "CursorOrigin");
            initat.graphicRaycaster = canv.GetComponent<GraphicRaycaster>();
            initat.screenSize = canv.GetComponent<CanvasScaler>().referenceResolution;
            canv.gameObject.SetActive(true);
            InputManager.Instance.ActivateActionSet("Interface");
            IEnumerator ThisIsFoxoSaying()
            {
                yield return new WaitForSecondsRealtime(5.580f);
                CoreGameManager.Instance?.audMan?.PlaySingle(Foxo.audios.Get<SoundObject>("graduations"));
                yield break;
            }
            CoreGameManager.Instance.audMan.PlaySingle(Foxo.audios.Get<SoundObject>("graduated"));
            CoreGameManager.Instance.StartCoroutine(ThisIsFoxoSaying());
            initat.currentCursor.transform.SetSiblingIndex(but.transform.GetSiblingIndex() + 1);
        }
        [HarmonyPatch(typeof(PlaceholderWinManager), nameof(PlaceholderWinManager.BeginPlay)), HarmonyPrefix]
        static bool FoxoEndPlay(PlaceholderWinManager __instance, ref MovementModifier ___moveMod)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (foxoinF3) {
                CoreGameManager.Instance.GetPlayer(0).Am.moveMods.Add(___moveMod);
                CoreGameManager.Instance.GetPlayer(0).itm.Disable(true);
                __instance.gameObject.GetComponent<VideoPlayer>().Play();
                CoreGameManager.Instance.disablePause = true;
            }
            return !foxoinF3;
        }

        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.Initialize)), HarmonyPostfix]
        static void SwapApplesAndExtinguishersInventory()
        {
            CoreGameManager.Instance.musicMan.pitchModifier = 1f;
            if (TeacherManager.Instance == null) return;
            var mainteach = AccessTools.DeclaredField(typeof(TeacherManager), "<MainTeacherPrefab>k__BackingField").GetValue(TeacherManager.Instance) as Teacher; // I took it from UnityExplorer
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                var player = CoreGameManager.Instance.GetPlayer(i);
                CoreGameManager.Instance.GetPlayer(i).itm.items.DoIf(x => x.itemType == global::Items.Apple, x =>
                {
                    if (mainteach?.GetComponent<Foxo>() != null)
                        player.itm.SetItem(FoxoPlugin.ItemAssets.Get<ItemObject>("FireExtinguisher"), player.itm.items.ToList().IndexOf(x));
                    else if (x == FoxoPlugin.ItemAssets.Get<ItemObject>("FireExtinguisher"))
                        player.itm.SetItem(ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value, player.itm.items.ToList().IndexOf(x));
                });
            }
        }

        [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.CreateItem)), HarmonyPostfix]
        static void SwapApplesAndExtinguishers(RoomController room, ItemObject item, Vector2 pos, ref Pickup __result)
        {
            if (TeacherManager.Instance == null) return;
            var mainteach = AccessTools.DeclaredField(typeof(TeacherManager), "<MainTeacherPrefab>k__BackingField").GetValue(TeacherManager.Instance) as Teacher; // I took it from UnityExplorer
            if (item.itemType == global::Items.Apple && mainteach?.GetComponent<Foxo>() != null)
                __result.AssignItem(FoxoPlugin.ItemAssets.Get<ItemObject>("FireExtinguisher"));
        }

        [HarmonyPatch(typeof(StorageLocker), "Start"), HarmonyPostfix] // I AM NOT PATCHING Pickup.AssignItem NOW.
        static void StoragePatch(ref Pickup[] ___pickup)
        {
            if (TeacherManager.Instance == null) return;
            var mainteach = AccessTools.DeclaredField(typeof(TeacherManager), "<MainTeacherPrefab>k__BackingField").GetValue(TeacherManager.Instance) as Teacher; // I took it from UnityExplorer
            for (int i = 0; i < ___pickup.Length; i++)
            {
                if (___pickup[i].item.itemType == global::Items.Apple && mainteach?.GetComponent<Foxo>() != null)
                    ___pickup[i].AssignItem(FoxoPlugin.ItemAssets.Get<ItemObject>("FireExtinguisher"));
                else if (___pickup[i].item == FoxoPlugin.ItemAssets.Get<ItemObject>("FireExtinguisher"))
                    ___pickup[i].AssignItem(ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value);

            }
        }

        [HarmonyPatch(typeof(PartyEvent), nameof(PartyEvent.Begin)), HarmonyPostfix]
        static void WhyTheFix(ref Pickup ___currentPickup)
        {
            if (TeacherManager.Instance == null || TeacherManager.Instance?.SpawnedMainTeacher == null) return;
            if (___currentPickup.item == ItemMetaStorage.Instance.Find(a => a.value.itemType == global::Items.Apple && a.flags.HasFlag(ItemFlags.NoUses)).value
                && TeacherManager.Instance?.SpawnedMainTeacher?.GetComponent<Foxo>() != null)
                ___currentPickup.AssignItem(FoxoPlugin.ItemAssets.Get<ItemObject>("FireExtinguisher"));
        }

        static FieldInfo ___baldiImage = AccessTools.DeclaredField(typeof(BaldiTV), "baldiImage");
        static FieldInfo ___baldiTvAudioManager = AccessTools.DeclaredField(typeof(BaldiTV), "baldiTvAudioManager");

        [HarmonyPatch(typeof(BaldiTV), "BaldiSpeaks", MethodType.Enumerator), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> GlitchOut(IEnumerable<CodeInstruction> i) => new CodeMatcher(i).Start()
            .MatchForward(true,
            new CodeMatch(OpCodes.Ldloc_1),
            new CodeMatch(CodeInstruction.LoadField(typeof(BaldiTV), "baldiTvAudioManager")),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(CodeInstruction.LoadField(AccessTools.Method(typeof(BaldiTV), "BaldiSpeaks", new Type[] { typeof(SoundObject) }).GetCustomAttribute<StateMachineAttribute>().StateMachineType, "sound")),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(AudioManager), nameof(AudioManager.QueueAudio), new Type[] { typeof(SoundObject) })))
            .ThrowIfInvalid("Something went wrong!")
            .Advance(1)
            .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(AccessTools.Method(typeof(BaldiTV), "BaldiSpeaks", new Type[] { typeof(SoundObject) }).GetCustomAttribute<StateMachineAttribute>().StateMachineType, "sound"),
            new CodeInstruction(OpCodes.Ldloc_1),
            Transpilers.EmitDelegate<Action<SoundObject, BaldiTV>>((sound, __instance) =>
            {
                var img = ___baldiImage.GetValue(__instance) as Image;
                var audman = ___baldiTvAudioManager.GetValue(__instance) as AudioManager;
                var foxoSprites = Foxo.sprites.GetAll<Sprite[]>().ToList().FindAll(x => x.ToList().Find(f => !f.name.ToLower().Contains("wrath") && !f.name.ToLower().Contains("notebook")));

                if (sound == Foxo.audios.Get<SoundObject>("WrathEventAud"))
                {
                    img.GetComponent<Animator>().enabled = false;
                    img.GetComponent<VolumeAnimator>().enabled = false;
                    IEnumerator FreakOut()
                    {
                        while (audman.QueuedAudioIsPlaying && img.enabled)
                        {
                            img.sprite = foxoSprites[UnityEngine.Random.RandomRangeInt(0, foxoSprites.Count)].First();
                            yield return null;
                        }
                        img.GetComponent<Animator>().enabled = true;
                        img.GetComponent<VolumeAnimator>().enabled = true;
                        yield break;
                    }
                    __instance.StartCoroutine(FreakOut());
                }
            }))
            .InstructionEnumeration();

        [HarmonyPatch(typeof(TimeOut), nameof(TimeOut.Begin)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void WrathOut()
        {
            if (TeacherManager.Instance?.SpawnedMainTeacher == null) return;
            if (TeacherManager.Instance.SpawnedMainTeacher.GetComponent<Foxo>() != null)
            {
                var foxo = TeacherManager.Instance.SpawnedMainTeacher.GetComponent<Foxo>();
                if (foxo.forceWrath)
                {
                    MusicManager.Instance.StopMidi();
                }
            }
        }

        [HarmonyPatch(typeof(TimeOut), "Update"), HarmonyPrefix]
        static bool WrathLights(ref bool ___active, ref EnvironmentController ___ec, 
            ref float ___lightOffRate, ref float ___timeToNextLight, ref List<Cell> ___lightsToTurnOff,
            ref float ___timeToNextAnger, ref float ___baldiAngerRate)
        {
            if (!___active || TeacherManager.Instance?.SpawnedMainTeacher == null) return true;
            if (TeacherManager.Instance.SpawnedMainTeacher.GetComponent<Foxo>() != null)
            {
                var foxo = TeacherManager.Instance.SpawnedMainTeacher.GetComponent<Foxo>();
                if (foxo.forceWrath)
                {
                    ___timeToNextAnger -= 0.05f * (Time.deltaTime * ___ec.NpcTimeScale);
                    ___timeToNextLight -= Time.deltaTime * ___ec.EnvironmentTimeScale;
                    CoreGameManager.Instance.musicMan.pitchModifier += 0.005f * (Time.deltaTime * ___ec.EnvironmentTimeScale);
                    if (___timeToNextAnger <= 0f)
                    {
                        ___timeToNextAnger = ___baldiAngerRate + ___timeToNextAnger;
                        foxo.GetAngry(___baldiAngerRate);
                    }

                    if (___timeToNextLight <= 0f)
                    {
                        ___timeToNextLight = ___lightOffRate + ___timeToNextLight;
                        if (___lightsToTurnOff.Count > 0)
                        {
                            ___lightsToTurnOff[0].lightColor = Color.red;
                            ___ec.QueueLightSourceForRegenerate(___lightsToTurnOff[0]);
                            ___lightsToTurnOff[0].SetLight(true);
                            ___lightsToTurnOff.RemoveAt(0);
                        }
                    }
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SubtitleController), nameof(SubtitleController.Initialize)), HarmonyPriority(Priority.Last)]
    class FoxoFont
    {
        static void Postfix(SubtitleController __instance)
        {
            if (Foxo.audios.GetAll<SoundObject>().Contains(__instance.soundObject) || Foxo.audios.GetAll<SoundObject[]>().ToList().Exists(snd => snd.Contains(__instance.soundObject)))
                __instance.text.font = Foxo.fonts.Get<TMP_FontAsset>("Cooper24");
        }
    }
}
