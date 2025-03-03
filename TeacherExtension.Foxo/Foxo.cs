using BepInEx.Bootstrap;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using TeacherAPI;
using TeacherAPI.utils;
using TeacherExtension.Foxo.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Video;
using UnityEngine.Networking;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Video;
using static UnityEngine.UIElements.UIR.BestFitAllocator;

namespace TeacherExtension.Foxo
{
    public class Foxo : Teacher
    {
        public static AssetManager sprites = new AssetManager();
        public static AssetManager audios = new AssetManager();
        public static AssetManager fonts = new AssetManager();
        public PlayerManager target;
        public bool forceWrath = false;
        internal System.Random jumpRNG = new System.Random();
        public float jumpChance => MovementPortion + (nextSlapDistance / Delay);

        // Foxo specifically uses a CustomSpriteAnimator
        public CustomSpriteAnimator animator;

        public static void LoadAssets()
        {
            if (FontEngine.LoadFontFace(Path.Combine(AssetLoader.GetModPath(FoxoPlugin.Instance), "COOPBL.TTF"), 24) != FontEngineError.Success)
            {
                MTM101BaldiDevAPI.CauseCrash(FoxoPlugin.Instance.Info, new System.Exception("Something went wrong loading the font file!"));
                return;
            } // Cool custom font!!
            Font font = new Font(Path.Combine(AssetLoader.GetModPath(FoxoPlugin.Instance), "COOPBL.TTF")); // CANNOT BE STATIC OR ELSE THE TEXT MESH PRO FONT WILL FAIL.
            var font24 = TMP_FontAsset.CreateFontAsset(font, 24, 2, UnityEngine.TextCore.LowLevel.GlyphRenderMode.RASTER_HINTED, 128, 256, AtlasPopulationMode.Dynamic, false);
            font24.name = "Cooper24";
            font24.atlasTexture.wrapMode = TextureWrapMode.Repeat;
            font24.atlasTexture.filterMode = FilterMode.Point;
            font24.atlasTexture.anisoLevel = 1;
            font24.MarkAsNeverUnload();
            var font18 = TMP_FontAsset.CreateFontAsset(font, 18, 2, UnityEngine.TextCore.LowLevel.GlyphRenderMode.RASTER_HINTED, 128, 256, AtlasPopulationMode.Dynamic, false);
            font18.name = "Cooper18";
            font18.atlasTexture.wrapMode = TextureWrapMode.Repeat;
            font18.atlasTexture.filterMode = FilterMode.Point;
            font18.atlasTexture.anisoLevel = 1;
            font18.MarkAsNeverUnload();
            var font14 = TMP_FontAsset.CreateFontAsset(font, 14, 2, UnityEngine.TextCore.LowLevel.GlyphRenderMode.RASTER_HINTED, 128, 256, AtlasPopulationMode.Dynamic, false);
            font14.name = "Cooper14";
            font14.atlasTexture.wrapMode = TextureWrapMode.Repeat;
            font14.atlasTexture.filterMode = FilterMode.Point;
            font14.atlasTexture.anisoLevel = 1;
            font14.MarkAsNeverUnload();
            fonts.AddRange<TMP_FontAsset>(new[] { font24, font18, font14 }, new string[]
            {
                "Cooper24",
                "Cooper18",
                "Cooper14"
            }); // I lied, there's more content.
            if (!(File.Exists(Path.Combine(AssetLoader.GetModPath(FoxoPlugin.Instance), "endings", "GradeCutscene_Good.mov"))
                && File.Exists(Path.Combine(AssetLoader.GetModPath(FoxoPlugin.Instance), "endings", "GradeCutscene.mov"))))
            {
                MTM101BaldiDevAPI.CauseCrash(FoxoPlugin.Instance.Info, new System.Exception("Video files does not exist!"));
                return;
            }
            var PIXELS_PER_UNIT = 30f;
            sprites.Add(
                "Wave",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "wave/Foxo_Wave{0:0000}.png", (0, 49))
                    .ToSprites(PIXELS_PER_UNIT)
            );
            sprites.Add(
                "Slap",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "slap{0}.png", (1, 4))
                    .ToSprites(PIXELS_PER_UNIT)
            );
            sprites.Add(
                "Sprayed",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "spray{0}.png", (1, 2))
                    .ToSprites(PIXELS_PER_UNIT)
            );
            sprites.Add(
                "Jump",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "jump{0}.png", (1, 2))
                    .ToSprites(PIXELS_PER_UNIT));
            sprites.Add(
                "Wrath",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "wrath{0}.png", (1, 3))
                    .ToSprites(PIXELS_PER_UNIT)
            );
            sprites.Add(
                "WrathSprayed",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "wrath_sprayed{0}.png", (1, 2))
                    .ToSprites(PIXELS_PER_UNIT)
            );
            sprites.Add(
                "Stare",
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(FoxoPlugin.Instance, "stare.png"), PIXELS_PER_UNIT)
            );
            sprites.Add(
                "Notebook",
                AssetLoader.TexturesFromMod(FoxoPlugin.Instance, "*.png", "comics").ToSprites(20f)
            );
            sprites.Add(
                "floor2Intro",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "floor2Intro{0}.png", (1, 2))
                    .ToSprites(PIXELS_PER_UNIT));
            sprites.AddRange(new Sprite[]
            {
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 50f, "items", "FireExtinguisher_Large.png"),
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 1f, "items", "FireExtinguisher_Small.png")
            }, new string[]
            {
                "Items/FireExtinguisher_Large",
                "Items/FireExtinguisher_Small",
            });
            sprites.Add("Graduated",
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 1f, "endings", "GraduationScreen.png"));

            // Shortcut functions
            AudioClip Clip(string path) => AssetLoader.AudioClipFromMod(FoxoPlugin.Instance, "audio", path);
            SoundObject NoSubtitle(AudioClip audio, SoundType type)
            {
                var snd = ObjectCreators.CreateSoundObject(audio, "", type, Color.white);
                snd.subtitle = false;
                return snd;
            };

            Color foxoSub = new Color(1f, 0.75f, 0f);
            audios.Add("boing", ObjectCreators.CreateSoundObject(Clip("boing.wav"), "Sfx_Foxo_Boing", SoundType.Effect, foxoSub));
            audios.Add("ding", ObjectCreators.CreateSoundObject(Clip("ding.wav"), "Sfx_Foxo_Ding", SoundType.Effect, foxoSub));
            audios.Add("school", NoSubtitle(Clip("school2.wav"), SoundType.Music));
            audios.Add("schoolnight", NoSubtitle(Clip("school2Night.wav"), SoundType.Music));
            audios.Add("hellothere", ObjectCreators.CreateSoundObject(Clip("hellothere.wav"), "Vfx_Foxo_Introduction", SoundType.Voice, foxoSub));
            audios.Add("slap", ObjectCreators.CreateSoundObject(Clip("slap.wav"), "Sfx_Foxo_Slap", SoundType.Effect, foxoSub));
            audios.Add("slap2", ObjectCreators.CreateSoundObject(Clip("slap2.wav"), "Sfx_Foxo_Wrath", SoundType.Effect, Color.gray));
            audios.Add("scare", NoSubtitle(Clip("scare.wav"), SoundType.Effect));
            audios.Add("scream", ObjectCreators.CreateSoundObject(Clip("scream.wav"), "Vfx_Foxo_Scream", SoundType.Voice, foxoSub));
            audios.Add("wrath", NoSubtitle(Clip("wrath.wav"), SoundType.Music));
            audios.Add("wrathscream", ObjectCreators.CreateSoundObject(Clip("scream_wrath.wav"), "Vfx_Foxo_WrathScream", SoundType.Voice, Color.black)); // Long ass caption.
            audios.Add("fear", NoSubtitle(Clip("fear.wav"), SoundType.Effect));

            audios.Add("praise", new SoundObject[] {
                                ObjectCreators.CreateSoundObject(Clip("praise1.wav"), "Vfx_Foxo_Praise1", SoundType.Voice, foxoSub),
                                ObjectCreators.CreateSoundObject(Clip("praise2.wav"), "Vfx_Foxo_Praise2", SoundType.Voice, foxoSub),
                        });
            audios.Add("teleport", ObjectCreators.CreateSoundObject(Clip("foxotp.wav"), "Sfx_Foxo_Teleport", SoundType.Effect, foxoSub));
            audios.Add("bettergrades", ObjectCreators.CreateSoundObject(Clip("BetterGrades.wav"), "Vfx_Foxo_Floor2Bad", SoundType.Voice, foxoSub));
            audios.Add("messedup", NoSubtitle(Clip("Floor3MessedUp.wav"), SoundType.Voice));
            audios.Add("WrathEventAud", ObjectCreators.CreateSoundObject(Clip("wrath_intro.wav"), "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", SoundType.Voice, foxoSub));
            audios.Add("fireextinguisher", NoSubtitle(Clip("FireExtinguisher.wav"), SoundType.Effect));
            audios.Add("graduated", NoSubtitle(Clip("graduation.wav"), SoundType.Music));
            audios.Add("graduations", ObjectCreators.CreateSoundObject(Clip("FoxGraduated.wav"), "Vfx_Foxo_GoodJob", SoundType.Voice, foxoSub)); 
            // For Arcade...
            audios.Add("wrath1", NoSubtitle(Clip("wrath1.wav"), SoundType.Music));
            audios.Add("wrath2", NoSubtitle(Clip("wrath2.wav"), SoundType.Music));
            audios.Add("wrath3", NoSubtitle(Clip("wrath3.wav"), SoundType.Music));
            audios.Add("wrath4", NoSubtitle(Clip("wrath4.wav"), SoundType.Music));
        }
        public bool IsBadFloor(int num, int deaths)
        {
            return !TeacherPlugin.IsEndlessFloorsLoaded() && num == BaseGameManager.Instance.CurrentLevel &&
                ((deaths <= FoxoPlugin.Instance.deathCounter.deaths) || (PlayerFileManager.Instance.lifeMode == LifeMode.Arcade && (num + 1) <= FoxoPlugin.Instance.deathCounter.deaths));
        }

        public bool IsBadEndlessFloor(int num, int deaths)
        {
            return TeacherPlugin.IsEndlessFloorsLoaded() && num <= BaseGameManager.Instance.CurrentLevel && deaths <= FoxoPlugin.Instance.deathCounter.deaths;
        }

        internal EnvironmentController.TempObstacleManagement unaccessibleMang;
        internal EnvironmentController.TempObstacleManagement accessibleMang;
        //public static EnvironmentController.TempObstacleManagement tempOpenSpecial { get; private set; }
        //public static EnvironmentController.TempObstacleManagement tempCloseSpecial { get; private set; }

        private void TempCloseSpecial()
        {
            ec.FreezeNavigationUpdates(true);
            foreach (var special in ec.rooms.FindAll(x => x.category == RoomCategory.Special))
            {
                foreach (var cell in special.cells)
                    for (int i = 0; i < 4; i++)
                        if (cell.ConstNavigable((Direction)i))
                            ec.CellFromPosition(cell.position + ((Direction)i).ToIntVector2()).Block(((Direction)i).GetOpposite(), true);

            }
            ec.FreezeNavigationUpdates(false);
        }

        private void TempOpenSpecial()
        {
            ec.FreezeNavigationUpdates(true);
            foreach (var special in ec.rooms.FindAll(x => x.category == RoomCategory.Special))
            {
                foreach (var cell in special.cells)
                    for (int i = 0; i < 4; i++)
                        if (cell.ConstNavigable((Direction)i))
                            ec.CellFromPosition(cell.position + ((Direction)i).ToIntVector2()).Block(((Direction)i).GetOpposite(), false);

            }
            ec.FreezeNavigationUpdates(false);
        }

        public override void Initialize()
        {
            base.Initialize();
            unaccessibleMang = (EnvironmentController.TempObstacleManagement)Delegate.Combine(unaccessibleMang, new EnvironmentController.TempObstacleManagement(TempCloseSpecial));
            accessibleMang = (EnvironmentController.TempObstacleManagement)Delegate.Combine(accessibleMang, new EnvironmentController.TempObstacleManagement(TempOpenSpecial));
            //navigator.passableObstacles.Add(FoxoPlugin.foxoUnpassable);

            // Appearance and sound
            {
                var waveSprites = sprites.Get<Sprite[]>("Wave");
                var slapSprites = sprites.Get<Sprite[]>("Slap");
                var wrathSprites = sprites.Get<Sprite[]>("Wrath");
                animator.animations.Add("Wave", new CustomAnimation<Sprite>(waveSprites, 3f));
                animator.animations.Add("Happy", new CustomAnimation<Sprite>(new Sprite[] { waveSprites[waveSprites.Length - 1] }, 1f));
                animator.animations.Add("Stare", new CustomAnimation<Sprite>((IsBadFloor(1, 2) || IsBadFloor(2, 2) || IsBadEndlessFloor(Mathf.RoundToInt((BaseGameManager.Instance.CurrentLevel + 1) / 2f), 3)) ? sprites.Get<Sprite[]>("floor2Intro") : new Sprite[] { sprites.Get<Sprite>("Stare") }, 0.02f));

                animator.animations.Add("Slap", new CustomAnimation<Sprite>(slapSprites, 1f));
                animator.animations.Add("SlapIdle", new CustomAnimation<Sprite>(new Sprite[] { slapSprites[slapSprites.Length - 1] }, 1f));
                animator.animations.Add("Sprayed", new CustomAnimation<Sprite>(Foxo.sprites.Get<Sprite[]>("Sprayed"), 0.1f));
                animator.animations.Add("Jump", new CustomAnimation<Sprite>(Foxo.sprites.Get<Sprite[]>("Jump"), 0.2f));
                //animator.animations.Add("JumpIdle", new CustomAnimation<Sprite>(new Sprite[] { Foxo.sprites.Get<Sprite[]>("Jump").Last() }, 1f));

                animator.animations.Add("WrathIdle", new CustomAnimation<Sprite>(new Sprite[] { wrathSprites[0] }, 1f));
                animator.animations.Add("Wrath", new CustomAnimation<Sprite>(wrathSprites.Reverse().ToArray(), 0.3f));
                animator.animations.Add("WrathSprayed", new CustomAnimation<Sprite>(Foxo.sprites.Get<Sprite[]>("WrathSprayed"), 0.02f));
                navigator.Entity.SetHeight(6.5f);
                AddLoseSound(audios.Get<SoundObject>("scare"), 1);
            }

            // Foxo specific
            {
                target = ec.Players[0];
                baseAnger += 1;
                baseSpeed *= 0.6f;
            }

            // Random events
            ReplaceEventText<RulerEvent>(audios.Get<SoundObject>("WrathEventAud"));
        }
        public override TeacherState GetAngryState() => forceWrath ? (Foxo_StateBase)(new Foxo_Wrath(this)) : new Foxo_Chase(this);
        public override TeacherState GetHappyState() => forceWrath ? (Foxo_StateBase)(new Foxo_WrathHappy(this)) : new Foxo_Happy(this);
        public override string GetNotebooksText(string amount) => $"{amount} Foxo Comics";
        public override WeightedTeacherNotebook GetTeacherNotebookWeight()
            => new WeightedTeacherNotebook(this).Weight(100).Sprite(sprites.Get<Sprite[]>("Notebook"));
        // Only play visual/audio effects, doesn't actually moves
        public new void SlapNormal()
        {
            animator.SetDefaultAnimation("SlapIdle", 1f);
            animator.Play("Slap", 1f);
            SlapRumble();
            AudMan.PlaySingle(audios.Get<SoundObject>("slap"));
        }
        public new void SlapBroken()
        {
            animator.SetDefaultAnimation("WrathIdle", 1f);
            animator.Play("Wrath", 1f);
            SlapRumble();
            AudMan.PlaySingle(audios.Get<SoundObject>("slap2"));
        }
        public override float DistanceCheck(float val)
        {
            if (animator.currentAnimationName.StartsWith("Jump") && navigator.Am.Multiplier != 0f)
            {
                float num = val * (1f / navigator.Am.Multiplier);
                if (slapTotal + num > slapDistance)
                {
                    if (slapTotal < slapDistance)
                    {
                        float num2 = (slapDistance - slapTotal) * navigator.Am.Multiplier;
                        slapTotal += num;
                        totalDistance += num2;
                        num2 += anger;
                        return num2;
                    }

                    slapTotal += num;
                    EndSlap();
                    return 0f;
                }

                slapTotal += num;
                totalDistance += val;
                return val;
            }
            return base.DistanceCheck(val);
        }

        // Ruler related events
        protected override void OnRulerBroken()
        {
            if (forceWrath) return;
            extraAnger += 3;
            behaviorStateMachine.ChangeState(new Foxo_Wrath(this));
        }
        protected override void OnRulerRestored()
        {
            if (forceWrath) return;
            behaviorStateMachine.ChangeState(new Foxo_Chase(this));
        }

        // Foxo specific
        public void TeleportToNearestDoor()
        {
            var playerPos = ec.Players[0].transform.position;
            Door nearestDoor = null;
            var nearest = float.PositiveInfinity;

            // Get nearest door
            foreach (var tile in ec.AllCells())
            {
                foreach (var door in tile.doors)
                {
                    var distance = (door.transform.position - playerPos).magnitude;
                    if (distance <= nearest)
                    {
                        nearestDoor = door;
                        nearest = distance;
                    }
                }
            }

            if (nearestDoor == null)
            {
                Debug.LogWarning("No nearest door found for Foxo");
                return;
            }

            // Get most far side
            Vector3 teleportPosition;
            if ((nearestDoor.aTile.Tile.transform.position - playerPos).magnitude < (nearestDoor.bTile.Tile.transform.position - playerPos).magnitude)
            {
                teleportPosition = nearestDoor.bTile.Tile.transform.position;
            }
            else
            {
                teleportPosition = nearestDoor.aTile.Tile.transform.position;
            }
            transform.position = teleportPosition + Vector3.up * 5f;
        }
        public void Sprayed()
        {
            behaviorStateMachine.ChangeState(new Foxo_Extinguished(this, behaviorStateMachine.currentState as TeacherState));
        }
        public void Jump()
        {
            animator.SetDefaultAnimation("SlapIdle", 1f);
            animator.Play("Jump", 1f);
            navigator.Entity.SetHeight(8f);
            //SlapRumble();
            AudMan.PlaySingle(audios.Get<SoundObject>("boing"));
        }
        protected override void VirtualUpdate()
        {
            base.VirtualUpdate();
            if (target.ruleBreak.ToLower() != "running" && target.plm.Entity.InternalMovement.magnitude <= 0f)
                target.plm.AddStamina(target.plm.staminaDrop * 0.8f * Time.deltaTime * target.PlayerTimeScale, true);
            if (!animator.currentAnimationName.StartsWith("Jump") && navigator.Entity.InternalHeight != 6.5f)
                navigator.Entity.SetHeight(6.5f);
        }

    }
    public class Foxo_StateBase : TeacherState
    {
        public Foxo_StateBase(Foxo foxo) : base(foxo)
        {
            this.foxo = foxo;
        }
        protected virtual void ActivateSlapAnimation() { }
        protected Foxo foxo;
    }
    public class Foxo_Happy : Foxo_StateBase
    {
        public Foxo_Happy(Foxo foxo) : base(foxo) { }
        public override void Enter()
        {
            base.Enter();
            ogPos = foxo.spriteBase.transform.localPosition;
            int floor = BaseGameManager.Instance.CurrentLevel + 1;
            bool infbad = foxo.IsBadEndlessFloor(Mathf.RoundToInt(floor / 2f), 3);
            bool infmessed = foxo.IsBadEndlessFloor(Mathf.RoundToInt(floor / 4f), 6);
            bool infwrath = foxo.IsBadEndlessFloor(Mathf.RoundToInt(floor / 6f), 10);
            if (!((foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || foxo.IsBadFloor(2, 4)
                || infbad || infmessed || infwrath || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel > 0))
                && !foxo.IsHelping()))
            {
                foxo.animator.Play("Wave", 1f);
                foxo.animator.SetDefaultAnimation("Happy", 1f);
                foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("hellothere"));
            }
            else if (infwrath || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 2))
            {
                foreach (var light in BaseGameManager.Instance.Ec.lights)
                    light.SetPower(false);
                foxo.disableNpcs = true;
                foxo.forceWrath = true;
                foxo.behaviorStateMachine.ChangeState(foxo.GetHappyState());
                return;
            }
            else if ((foxo.IsBadFloor(2, 4) || infmessed || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 1))
                && !(infwrath || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 2)))
            {
                foreach (var light in BaseGameManager.Instance.Ec.lights)
                    light.SetLight(!(light.room.category != RoomCategory.Special));
                Cell cell = foxo.ec.RandomCell(false, false, true);
                while ((cell.CenterWorldPosition - foxo.players[0].transform.position).magnitude < 111f && cell.room.category != RoomCategory.Hall)
                    cell = foxo.ec.RandomCell(false, false, true);
                foxo.Navigator.Entity.Teleport(cell.CenterWorldPosition);
                foxo.StartCoroutine(JustWaitForGameToStart());
            }
            else if ((foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || infbad)
                && !foxo.IsBadFloor(2, 4) 
                && !(infmessed || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 1)))
            {
                foxo.animator.Play("Stare", 1f);
                foxo.animator.SetDefaultAnimation("Stare", 1f);
                foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("bettergrades"));
                foxo.StartCoroutine(cutsceneFloor2());
            }
            foxo.Navigator.SetSpeed(0f);
            ChangeNavigationState(new NavigationState_DoNothing(foxo, 32));

            if (!(foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || foxo.IsBadFloor(2, 4) || infbad || infmessed || infwrath || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel > 0)))
                if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.seasons"))
                    foxo.ReplacementMusic = SeasonCycleStuff.CheckIfNight() ? Foxo.audios.Get<SoundObject>("schoolnight") : Foxo.audios.Get<SoundObject>("school");
                else
                    foxo.ReplacementMusic = Foxo.audios.Get<SoundObject>("school");
            else
                foxo.ReplacementMusic = "mute";
        }
        private Vector3 ogPos;

        private IEnumerator cutsceneFloor2()
        {
            yield return new WaitForSecondsEnvironmentTimescale(foxo.ec, 0.655f);
            float time = 0f;
            while (foxo.spriteBase.transform.localPosition.y > -10f)
            {
                time += (0.00015f + (time / 15f)) * (Time.timeScale * foxo.ec.EnvironmentTimeScale);
                foxo.spriteBase.transform.localPosition = Vector3.Lerp(ogPos, Vector3.down * 10f, time);
                yield return null;
            }
            yield break;
        }

        private IEnumerator JustWaitForGameToStart()
        {
            yield return new WaitUntil(() => foxo.ec.Active);
            CoreGameManager.Instance.audMan.PlaySingle(Foxo.audios.Get<SoundObject>("messedup"));
            foxo.AudMan.audioDevice.reverbZoneMix = 1;
            var reverb = foxo.gameObject.AddComponent<AudioReverbZone>();
            reverb.minDistance = 500f;
            reverb.maxDistance = 1000f;
            reverb.reverbPreset = AudioReverbPreset.Hallway;
            foxo.disableNpcs = true;
            foxo.baseAnger += 1;
            foxo.baseSpeed *= 0.6f;
            foxo.ActivateSpoopMode();
            foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
        }

        public override void Exit()
        {
            foxo.spriteBase.transform.localPosition = ogPos;
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            base.NotebookCollected(currentNotebooks, maxNotebooks);
            if (foxo.IsHelping())
            {
                foxo.ActivateSpoopMode();
                foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
                CoreGameManager.Instance.audMan.PlaySingle(Foxo.audios.Get<SoundObject>("ding"));
                return;
            }
            foxo.behaviorStateMachine.ChangeState(new Foxo_Scary(foxo));
        }
    }
    public class Foxo_Scary : Foxo_StateBase
    {
        public Foxo_Scary(Foxo foxo) : base(foxo) { }
        public override void Enter()
        {
            base.Enter();
            int floor = BaseGameManager.Instance.CurrentLevel + 1;
            bool infbad = foxo.IsBadEndlessFloor(Mathf.RoundToInt(floor / 2f), 3);
            bool infmessed = foxo.IsBadEndlessFloor(Mathf.RoundToInt(floor / 4f), 6);
            bool infwrath = foxo.IsBadEndlessFloor(Mathf.RoundToInt(floor / 6f), 10);
            if (!(foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || foxo.IsBadFloor(2, 4) || infbad || infmessed || infwrath))
            {
                foxo.animator.SetDefaultAnimation("Stare", 1f);
                foxo.animator.Play("Stare", 1f);

                // Stop playing songs and be scary for once!!
                MusicManager.Instance.StopMidi();
                foxo.ec.lightMode = LightMode.Greatest;
                foxo.ec.standardDarkLevel = Color.black;
                CoreGameManager.Instance.musicMan.FlushQueue(true);
                CoreGameManager.Instance.musicMan.PlaySingle(Foxo.audios.Get<SoundObject>("fear"));
                foxo.ec.FlickerLights(true);
                foxo.TeleportToNearestDoor();

                foxo.StartCoroutine(GetMad());
            }
            else if ((foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || infbad) && !foxo.IsBadFloor(2, 4) && !infmessed)
            {
                Cell cell = foxo.ec.RandomCell(false, false, true);
                while ((cell.CenterWorldPosition - foxo.players[0].transform.position).magnitude < 111f)
                    cell = foxo.ec.RandomCell(false, false, true);
                foxo.Navigator.Entity.Teleport(cell.CenterWorldPosition);
                foxo.AudMan.audioDevice.reverbZoneMix = 1;
                var reverb = foxo.gameObject.AddComponent<AudioReverbZone>();
                reverb.minDistance = 250f;
                reverb.maxDistance = 500f;
                reverb.reverbPreset = AudioReverbPreset.Hallway;
                foxo.ActivateSpoopMode();
                foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
            }
        }
        private IEnumerator GetMad()
        {
            yield return new WaitForSeconds(13f);
            foxo.ec.FlickerLights(false);
            AudioManager aud = foxo.ec.ReflectionGetVariable("audMan") as AudioManager;
            aud.PlaySingle(Foxo.audios.Get<SoundObject>("ding"));
            foxo.ActivateSpoopMode();
            foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
            yield break;
        }
    }
    public class Foxo_Chase : Foxo_StateBase
    {
        protected float delayTimer;
        public Foxo_Chase(Foxo foxo) : base(foxo) { }
        public override void OnStateTriggerStay(Collider other)
        {
            if (foxo.IsTouchingPlayer(other))
            {
                foxo.CaughtPlayer(foxo.target);
            }
        }
        public override void GoodMathMachineAnswer()
        {
            if (foxo.forceWrath || foxo.behaviorStateMachine.currentState.GetType().Equals(typeof(Foxo_Wrath))) return;
            foxo.behaviorStateMachine.ChangeState(new Foxo_Praise(foxo, this));
        }
        public override void Enter()
        {
            base.Enter();
            foxo.animator.SetDefaultAnimation("SlapIdle", 1f);
            delayTimer = foxo.Delay;
            foxo.ResetSlapDistance();
        }
        public override void Update()
        {
            base.Update();
            foxo.UpdateSlapDistance();
            delayTimer -= Time.deltaTime * npc.TimeScale;

            if (foxo.forceWrath && GetType().Equals(typeof(Foxo_Chase)))
            {
                foxo.behaviorStateMachine.ChangeState(new Foxo_Wrath(foxo));
                return;
            }

            if (delayTimer <= 0f)
            {
                // Progressive restoration after wrath
                if ((float)foxo.ReflectionGetVariable("extraAnger") > 0 && GetType().Equals(typeof(Foxo_Chase)))
                    foxo.ReflectionSetVariable("extraAnger", (float)foxo.ReflectionGetVariable("extraAnger") - 1);

                //foxo.ec.FindPath(foxo.ec.CellFromPosition(foxo.transform.position), foxo.ec.CellFromPosition(foxo.Navigator.CurrentDestination), PathType.Nav, out List<Cell> paths, out bool suc);
                // This is not fun...
                if (foxo.Navigator.Entity.CurrentRoom?.category == RoomCategory.Special && currentNavigationState.GetType().Equals(typeof(NavigationState_TargetPlayer)))
                {
                    Cell newcell = foxo.ec.RandomCell(false, true, true);
                    while (newcell.room.category != RoomCategory.Hall || Vector3.Distance(newcell.CenterWorldPosition, foxo.target.transform.position) < 150f)
                        newcell = foxo.ec.RandomCell(false, true, true);
                    foxo.Navigator.Entity.Teleport(newcell.CenterWorldPosition);
                    foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("teleport"));
                }
                // Foxo always know where the player is, except in special rooms
                foxo.unaccessibleMang?.Invoke();
                if (!((foxo.target?.GetComponent<PlayerEntity>()?.CurrentRoom != null && foxo.target.GetComponent<PlayerEntity>().CurrentRoom?.category == RoomCategory.Special)
                    /*|| (suc && paths.Exists(x => x.room.category == RoomCategory.Special))*/))
                    ChangeNavigationState(new NavigationState_TargetPlayer(foxo, 0, foxo.target.transform.position));
                else if (!currentNavigationState.GetType().Equals(typeof(NavigationState_WanderRandom)) /*|| (suc && paths.Exists(x => x.room.category == RoomCategory.Special))*/)
                {
                    //if (suc && paths.Exists(x => x.room.category == RoomCategory.Special)) foxo.Navigator.ClearCurrentDirs();
                    ChangeNavigationState(new NavigationState_WanderRandom(foxo, 0));
                }
                foxo.accessibleMang?.Invoke();

                foxo.Slap();
                ActivateSlapAnimation();
                delayTimer = foxo.Delay;
            }
        }
        protected override void ActivateSlapAnimation()
        {
            if (foxo.jumpRNG.NextDouble() < (double)foxo.jumpChance)
                foxo.Jump();
            else
                foxo.SlapNormal();
        }
    }
    public class Foxo_Praise : Foxo_StateBase
    {
        public TeacherState previousState;
        private float time;

        public Foxo_Praise(Foxo foxo, TeacherState previousState) : base(foxo)
        {
            this.previousState = previousState;
            time = 4f;
        }
        public override void Enter()
        {
            base.Enter();
            foxo.AudMan.PlayRandomAudio(Foxo.audios.Get<SoundObject[]>("praise"));
            foxo.animator.SetDefaultAnimation("Happy", 1f);
            foxo.animator.Play("Happy", 1f);
        }
        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0) foxo.behaviorStateMachine.ChangeState(previousState);
        }
    }
    public class Foxo_Extinguished : Foxo_StateBase
    {
        public TeacherState previousState;
        private float time = 15f;
        public Foxo_Extinguished(Foxo foxo, TeacherState previousState) : base(foxo) { this.previousState = previousState; }

        public override void Enter()
        {
            base.Enter();
            if (previousState.GetType().Equals(typeof(Foxo_Wrath)) || foxo.forceWrath)
            {
                foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("wrathscream"));
                foxo.animator.SetDefaultAnimation("WrathSprayed", 1f);
                foxo.animator.Play("WrathSprayed", 1f);
                return;
            }
            foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("scream"));
            foxo.animator.SetDefaultAnimation("Sprayed", 1f);
            foxo.animator.Play("Sprayed", 1f);
        }

        public override void Update()
        {
            time -= Time.deltaTime * foxo.ec.EnvironmentTimeScale;
            if (!foxo.AudMan.AnyAudioIsPlaying && time <= 0f)
                foxo.behaviorStateMachine.ChangeState(previousState);

        }
    }

    public class Foxo_WrathHappy : Foxo_StateBase
    {
        public Foxo_WrathHappy(Foxo foxo) : base(foxo) { }

        public override void Initialize()
        {
            base.Initialize();
            var lanternmode = foxo.ec.gameObject.GetOrAddComponent<LanternMode>();
            if ((EnvironmentController)lanternmode.ReflectionGetVariable("ec") == null) lanternmode.Initialize(foxo.ec);
            lanternmode.AddSource(foxo.players[0].transform, 5.5f, Color.white);
        }

        public override void Enter()
        {
            base.Enter();
            foxo.animator.SetDefaultAnimation("WrathIdle", 1f);
            foxo.Navigator.SetSpeed(0f);
            foxo.spriteBase.SetActive(false);
            ChangeNavigationState(new NavigationState_DoNothing(foxo, 32));
            //foxo.ReplaceMusic();
            foxo.ReplacementMusic = "mute";
            Cell cell = foxo.ec.RandomCell(false, false, true);
            while ((cell.CenterWorldPosition - foxo.players[0].transform.position).magnitude < 122f)
                cell = foxo.ec.RandomCell(false, false, true);
            foxo.Navigator.Entity.Teleport(cell.CenterWorldPosition);
            IEnumerator WaitForGameToStart()
            {
                while (!foxo.ec.Active)
                    yield return null;
                foxo.ActivateSpoopMode();
                foxo.behaviorStateMachine.ChangeState(new Foxo_Wrath(foxo));
            }
            foxo.StartCoroutine(WaitForGameToStart());
        }
        public override void Exit()
        {
            base.Exit();
            foxo.ec.lightMode = LightMode.Greatest;
            foxo.ec.standardDarkLevel = Color.black;
            foxo.spriteBase.SetActive(true);
        }
        public override void NotebookCollected(int c, int m)
        {
            base.NotebookCollected(c, m);
            foxo.ActivateSpoopMode();
            foxo.behaviorStateMachine.ChangeState(new Foxo_Wrath(foxo));
        }
    }
    public class Foxo_Wrath : Foxo_Chase
    {
        public Foxo_Wrath(Foxo foxo) : base(foxo) { }
        protected override void ActivateSlapAnimation()
        {
            // 6 Lines of code for a sound effect 💀
            if (!isBroken)
            {
                foxo.AudMan.PlaySingle(TeacherPlugin.Instance.CurrentBaldi.ReflectionGetVariable("rulerBreak") as SoundObject);
                isBroken = true;
            }
            foxo.SlapBroken();
        }
        public override void Initialize()
        {
            base.Initialize();
            if (foxo.forceWrath && !foxo.IsHelping())
            {
                foxo.AudMan.audioDevice.reverbZoneMix = 1;
                var reverb = foxo.ec.gameObject.AddComponent<AudioReverbZone>();
                reverb.minDistance = 500f;
                reverb.maxDistance = 1000f;
                reverb.reverbPreset = AudioReverbPreset.Hallway;
                CoreGameManager.Instance.musicMan.pitchModifier = 0.66f;
                CoreGameManager.Instance.musicMan.QueueAudio(Foxo.audios.Get<SoundObject>($"wrath{new System.Random(CoreGameManager.Instance.Seed() + CoreGameManager.Instance.sceneObject.levelNo).Next(1, 5)}"), true);
                CoreGameManager.Instance.musicMan.SetLoop(true);
                return;
            }
            AudioManager aud = foxo.ec.ReflectionGetVariable("audMan") as AudioManager;
            aud.PlaySingle(Foxo.audios.Get<SoundObject>("wrath"));
        }
        public override void Enter()
        {
            base.Enter();
            foxo.ec.FlickerLights(false);
        }
        public override void Exit()
        {
            base.Exit();
            foxo.ec.FlickerLights(false);
        }

        private bool isBroken = false;
    }
}
