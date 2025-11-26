using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TeacherAPI;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TeacherExtension.Foxo
{
    public class Foxo : Teacher
    {
        public static readonly AssetManager foxoAssets = new AssetManager();
        public PlayerManager target;
        public bool forceWrath = false;
        internal System.Random jumpRNG = new System.Random();
        public float jumpChance => playerDistance / 1000f;
        private float playerDistance;

        // Max of 8 deaths from all floors (2 deaths per floor)
        internal bool IsBadPhase1 => IsBadFloor(1, 2) || IsBadFloor(2, 2) || IsBadFloor(3, 4) || IsBadFloor(4, 4) || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 2);
        internal bool IsBadPhase2 => IsBadFloor(2, 4) || IsBadFloor(3, 6) || IsBadFloor(4, 6) || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 3);
        internal bool IsBadPhase3 => (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 4);

        // Foxo specifically uses a CustomSpriteAnimator
        [SerializeField] internal CustomSpriteRendererAnimator animator;

        public static void LoadAssets()
        {
            if (FontEngine.LoadFontFace(Path.Combine(AssetLoader.GetModPath(FoxoPlugin.Instance), "COOPBL.TTF"), 24) != FontEngineError.Success)
            {
                MTM101BaldiDevAPI.CauseCrash(FoxoPlugin.Instance.Info, new System.Exception("Something went wrong loading the font file!"));
                return;
            } // Cool custom font!!
            // CANNOT BE STATIC OR ELSE THE TEXT MESH PRO FONT WILL FAIL
            var font24 = AssetLoader.TMPAssetFromMod(FoxoPlugin.Instance, new string[] { "COOPBL.TTF" }, 24, 2, GlyphRenderMode.RASTER_HINTED, 128, 256, AtlasPopulationMode.Dynamic);
            font24.name = "Cooper24";
            font24.atlasTexture.wrapMode = TextureWrapMode.Repeat;
            font24.atlasTexture.filterMode = FilterMode.Point;
            font24.atlasTexture.anisoLevel = 1;
            font24.MarkAsNeverUnload();
            var font18 = AssetLoader.TMPAssetFromMod(FoxoPlugin.Instance, new string[] { "COOPBL.TTF" }, 18, 2, GlyphRenderMode.RASTER_HINTED, 128, 256, AtlasPopulationMode.Dynamic);
            font18.name = "Cooper18";
            font18.atlasTexture.wrapMode = TextureWrapMode.Repeat;
            font18.atlasTexture.filterMode = FilterMode.Point;
            font18.atlasTexture.anisoLevel = 1;
            font18.MarkAsNeverUnload();
            var font14 = AssetLoader.TMPAssetFromMod(FoxoPlugin.Instance, new string[] { "COOPBL.TTF" }, 14, 2, GlyphRenderMode.RASTER_HINTED, 128, 256, AtlasPopulationMode.Dynamic);
            font14.name = "Cooper14";
            font14.atlasTexture.wrapMode = TextureWrapMode.Repeat;
            font14.atlasTexture.filterMode = FilterMode.Point;
            font14.atlasTexture.anisoLevel = 1;
            font14.MarkAsNeverUnload();
            foxoAssets.AddRange<TMP_FontAsset>(new[] { font24, font18, font14 }, new string[]
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
            foxoAssets.Add("PosterBase", AssetLoader.TextureFromMod(FoxoPlugin.Instance, "poster.png"));
            var PIXELS_PER_UNIT = 30f;
            foxoAssets.Add(
                "Wave",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "Foxo_Wave****.png", "wave")
                    .ToSprites(PIXELS_PER_UNIT)
            );
            foxoAssets.Add(
                "Slap",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "slap*.png")
                    .ToSprites(PIXELS_PER_UNIT)
            );
            foxoAssets.Add(
                "Sprayed",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "spray*.png")
                    .ToSprites(PIXELS_PER_UNIT)
            );
            foxoAssets.Add(
                "Jump",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "jump*.png")
                    .ToSprites(PIXELS_PER_UNIT));
            foxoAssets.Add(
                "Wrath",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "wrath*.png")
                    .ToSprites(PIXELS_PER_UNIT)
            );
            foxoAssets.Add(
                "WrathSprayed",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "wrath_sprayed*.png")
                    .ToSprites(PIXELS_PER_UNIT)
            );
            foxoAssets.Add(
                "Stare",
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(FoxoPlugin.Instance, "stare.png"), PIXELS_PER_UNIT)
            );
            foxoAssets.Add(
                "Notebook",
                AssetLoader.TexturesFromMod(FoxoPlugin.Instance, "*.png", "comics").ToSprites(20f)
            );
            foxoAssets.Add(
                "floor2Intro",
                AssetLoader
                    .TexturesFromMod(FoxoPlugin.Instance, "floor2Intro*.png")
                    .ToSprites(PIXELS_PER_UNIT));
            foxoAssets.AddRange(new Sprite[]
            {
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 50f, "items", "FireExtinguisher_Large.png"),
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 1f, "items", "FireExtinguisher_Small.png"),
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 50f, "items", "BucketWaterBucket_Large.png"),
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 1f, "items", "BucketWaterBucket_Small.png")
            }, new string[]
            {
                "Items/FireExtinguisher_Large",
                "Items/FireExtinguisher_Small",
                "Items/WaterBucketOfWater_Large",
                "Items/WaterBucketOfWater_Small"
            });
            foxoAssets.Add("Graduated",
                AssetLoader.SpriteFromMod(FoxoPlugin.Instance, Vector2.one / 2f, 1f, "endings", "GraduationScreen.png"));

            // Shortcut functions
            AudioClip Clip(string path) => AssetLoader.AudioClipFromMod(FoxoPlugin.Instance, "audio", path);

            Color foxoSub = new Color(1f, 0.75f, 0f);
            foxoAssets.Add("boing", ObjectCreators.CreateSoundObject(Clip("boing.wav"), "Sfx_Foxo_Boing", SoundType.Effect, foxoSub));
            foxoAssets.Add("ding", ObjectCreators.CreateSoundObject(Clip("ding.wav"), "Sfx_Foxo_Ding", SoundType.Effect, foxoSub));
            foxoAssets.Add("school", ObjectCreators.CreateSoundObject(Clip("school2.wav"), "Mfx_mus_foxoschool", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("schoolnight", ObjectCreators.CreateSoundObject(Clip("school2Night.wav"), "Mfx_mus_foxoschoolnight", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("hellothere", ObjectCreators.CreateSoundObject(Clip("hellothere.wav"), "Vfx_Foxo_Introduction", SoundType.Voice, foxoSub));
            foxoAssets.Add("slap", ObjectCreators.CreateSoundObject(Clip("slap.wav"), "Sfx_Foxo_Slap", SoundType.Effect, foxoSub));
            foxoAssets.Add("slap2", ObjectCreators.CreateSoundObject(Clip("slap2.wav"), "Sfx_Foxo_Wrath", SoundType.Effect, Color.gray));
            foxoAssets.Add("scare", ObjectCreators.CreateSoundObject(Clip("scare.wav"), "Sfx_Foxo_Jumpscare", SoundType.Effect, Color.white, 0f));
            foxoAssets.Add("scream", ObjectCreators.CreateSoundObject(Clip("scream.wav"), "Vfx_Foxo_Scream", SoundType.Voice, foxoSub));
            foxoAssets.Add("wrath", ObjectCreators.CreateSoundObject(Clip("wrath.wav"), "Mfx_mus_foxowrath", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("wrathscream", ObjectCreators.CreateSoundObject(Clip("scream_wrath.wav"), "Vfx_Foxo_WrathScream", SoundType.Voice, Color.black)); // Long ass caption.
            foxoAssets.Add("fear", ObjectCreators.CreateSoundObject(Clip("fear.wav"), "Sfx_mus_foxofear", SoundType.Effect, Color.white, 0f));

            foxoAssets.Add("praise", new WeightedSoundObject[] {
                                new WeightedSoundObject()
                                {
                                    selection = ObjectCreators.CreateSoundObject(Clip("praise1.wav"), "Vfx_Foxo_Praise1", SoundType.Voice, foxoSub),
                                    weight = 100,
                                },
                                new WeightedSoundObject()
                                {
                                    selection = ObjectCreators.CreateSoundObject(Clip("praise2.wav"), "Vfx_Foxo_Praise2", SoundType.Voice, foxoSub),
                                    weight = 100,
                                }
                        });
            foxoAssets.Add("teleport", ObjectCreators.CreateSoundObject(Clip("foxotp.wav"), "Sfx_Foxo_Teleport", SoundType.Effect, foxoSub));
            foxoAssets.Add("bettergrades", ObjectCreators.CreateSoundObject(Clip("BetterGrades.wav"), "Vfx_Foxo_Floor2Bad", SoundType.Voice, foxoSub));
            foxoAssets.Add("messedup", ObjectCreators.CreateSoundObject(Clip("Floor3MessedUp.wav"), "Vfx_Foxo_Floor3Bad", SoundType.Voice, Color.white, 0f));
            foxoAssets.Add("WrathEventAud", ObjectCreators.CreateSoundObject(Clip("wrath_intro.wav"), "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", SoundType.Voice, foxoSub));
            foxoAssets.Add("fireextinguisher", ObjectCreators.CreateSoundObject(Clip("FireExtinguisher.wav"), "Vfx_FireExtinguisher", SoundType.Effect, Color.white, 0f));
            foxoAssets.Add("graduated", ObjectCreators.CreateSoundObject(Clip("graduation.wav"), "Mfx_graduation", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("graduations", ObjectCreators.CreateSoundObject(Clip("FoxGraduated.wav"), "Vfx_Foxo_GoodJob", SoundType.Voice, foxoSub)); 
            // For Arcade...
            foxoAssets.Add("wrath1", ObjectCreators.CreateSoundObject(Clip("wrath1.wav"), "Mfx_FoxoWrath1", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("wrath2", ObjectCreators.CreateSoundObject(Clip("wrath2.wav"), "Mfx_FoxoWrath2", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("wrath3", ObjectCreators.CreateSoundObject(Clip("wrath3.wav"), "Mfx_FoxoWrath3", SoundType.Music, Color.white, 0f));
            foxoAssets.Add("wrath4", ObjectCreators.CreateSoundObject(Clip("wrath4.wav"), "Mfx_FoxoWrath4", SoundType.Music, Color.white, 0f));
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

        internal EnvironmentController.TempObstacleManagement unaccessibleMang, accessibleMang;
        //public static EnvironmentController.TempObstacleManagement tempOpenSpecial { get; private set; }
        //public static EnvironmentController.TempObstacleManagement tempCloseSpecial { get; private set; }

        private void Block(bool block)
        {
            foreach (var special in ec.rooms.FindAll(x => x.category == RoomCategory.Special && x.functions.GetComponent<SpecialRoomSwingingDoorsBuilder>() != null))
            {
                foreach (var cell in special.cells.Where(c => !c.hideFromMap && !c.offLimits))
                    for (int i = 0; i < 4; i++)
                        if (cell.ConstNavigable((Direction)i))
                            ec.CellFromPosition(cell.position + ((Direction)i).ToIntVector2()).Block(((Direction)i).GetOpposite(), block);

            }
        }

        private void TempCloseSpecial()
        {
            ec.FreezeNavigationUpdates(true);
            Block(true);
            ec.FreezeNavigationUpdates(false);
        }

        private void TempOpenSpecial()
        {
            ec.FreezeNavigationUpdates(true);
            Block(false);
            ec.FreezeNavigationUpdates(false);
        }

        public override AssistantPolicy GetAssistantPolicy() => forceWrath ? new AssistantPolicy(PossibleAssistantAllowType.Allow).MaxAssistants(0) : base.GetAssistantPolicy();
        public override void Initialize()
        {
            base.Initialize();
            unaccessibleMang += TempCloseSpecial;
            accessibleMang += TempOpenSpecial;
            //navigator.passableObstacles.Add(FoxoPlugin.foxoUnpassable);

            // Appearance and sound
            {
                animator.AddAnimation("Stare", new SpriteAnimation((IsBadPhase1 || IsBadEndlessFloor(Mathf.RoundToInt((BaseGameManager.Instance.CurrentLevel + 1) / 2f), 3)) ? foxoAssets.Get<Sprite[]>("floor2Intro") : new Sprite[] { foxoAssets.Get<Sprite>("Stare") }, 0.02f));
                navigator.Entity.SetHeight(6.5f);
                AddLoseSound(foxoAssets.Get<SoundObject>("scare"), 1);
            }

            // Foxo specific
            {
                target = ec.Players[0];
                baseAnger += 1;
                baseSpeed *= 0.6f;
            }

            // Random events
            ReplaceEventText<RulerEvent>(foxoAssets.Get<SoundObject>("WrathEventAud"));
        }
        public override TeacherState GetAngryState() => forceWrath ? (Foxo_StateBase)(new Foxo_Wrath(this)) : new Foxo_Chase(this);
        public override TeacherState GetHappyState() => forceWrath ? (Foxo_StateBase)(new Foxo_WrathHappy(this)) : new Foxo_Happy(this);
        public override TeacherState GetPraiseState(float time) => (forceWrath || behaviorStateMachine.currentState.GetType().Equals(typeof(Foxo_Wrath)) || behaviorStateMachine.currentState.GetType().Equals(typeof(Foxo_WrathHappy))) ? (Foxo_StateBase)((Baldi_Praise)behaviorStateMachine.currentState).GetPreviousBaldiState() : new Foxo_Praise(this, (Foxo_StateBase)((Baldi_Praise)behaviorStateMachine.currentState).GetPreviousBaldiState(), time);
        public override string GetNotebooksText(string amount) => $"{amount} Foxo Comics";
        public override WeightedTeacherNotebook GetTeacherNotebookWeight()
            => new WeightedTeacherNotebook(this).Weight(100).Sprite(foxoAssets.Get<Sprite[]>("Notebook"));
        // Only play visual/audio effects, doesn't actually moves
        public new void SlapNormal()
        {
            animator.SetDefaultAnimation("SlapIdle", 1f);
            animator.Play("Slap", 1f);
            SlapRumble();
            AudMan.PlaySingle(slap);
        }
        public new void SlapBroken()
        {
            animator.SetDefaultAnimation("WrathIdle", 1f);
            animator.Play("Wrath", 1f);
            SlapRumble();
            AudMan.PlaySingle(foxoAssets.Get<SoundObject>("slap2"));
        }
        public override float DistanceCheck(float val)
        {
            if (animator.AnimationId.StartsWith("Jump") && navigator.Am.Multiplier != 0f)
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
            if (behaviorStateMachine.currentState.GetType().Equals(GetHappyState().GetType())) return;
            behaviorStateMachine.ChangeState(new Foxo_Extinguished(this, behaviorStateMachine.currentState as TeacherState));
        }
        public void Jump()
        {
            animator.SetDefaultAnimation("SlapIdle", 1f);
            animator.Play("Jump", 1f);
            navigator.Entity.SetHeight(8f);
            //SlapRumble();
            AudMan.PlaySingle(foxoAssets.Get<SoundObject>("boing"));
        }
        protected override void VirtualUpdate()
        {
            base.VirtualUpdate();
            playerDistance = Vector3.Distance(transform.position, target.transform.position);
            if (target.ruleBreak.ToLower() != "running" && target.plm.Entity.InternalMovement.magnitude <= 0f)
                target.plm.AddStamina(target.plm.staminaDrop * 0.8f * Time.deltaTime * target.PlayerTimeScale, true);
            if (!animator.AnimationId.StartsWith("Jump") && navigator.Entity.InternalHeight != 6.5f)
                navigator.Entity.SetHeight(6.5f);
        }

    }
    public class Foxo_StateBase : TeacherState
    {
        protected static FieldInfo _audMan = AccessTools.DeclaredField(typeof(EnvironmentController), "audMan");
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
            if (!((foxo.IsBadPhase1 || foxo.IsBadPhase2 || foxo.IsBadPhase3
                || infbad || infmessed || infwrath || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel > 0))
                && !foxo.IsHelping()))
            {
                foxo.animator.Play("Wave", 1f);
                foxo.animator.SetDefaultAnimation("Happy", 1f);
                foxo.AudMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("hellothere"));
            }
            else if (foxo.IsBadPhase3 || infwrath)
            {
                foreach (var light in BaseGameManager.Instance.Ec.lights)
                    light.SetPower(false);
                foxo.disableNpcs = true;
                foxo.forceWrath = true;
                foxo.behaviorStateMachine.ChangeState(foxo.GetHappyState());
                return;
            }
            else if ((foxo.IsBadPhase2 || infmessed)
                && !(foxo.IsBadPhase3 || infwrath))
            {
                foxo.animator.SetDefaultAnimation("Stare", 1f, true);
                foreach (var light in BaseGameManager.Instance.Ec.lights)
                    light.SetLight(!(light.room.category != RoomCategory.Special));
                Cell cell = foxo.ec.RandomCell(false, false, true);
                while ((cell.CenterWorldPosition - foxo.ec.Players[0].transform.position).magnitude < 111f && cell.room.category != RoomCategory.Hall)
                    cell = foxo.ec.RandomCell(false, false, true);
                foxo.Navigator.Entity.Teleport(cell.CenterWorldPosition);
                foxo.StartCoroutine(JustWaitForGameToStart());
            }
            else if ((foxo.IsBadPhase1 || infbad)
                && !(foxo.IsBadPhase2 && infmessed))
            {
                foxo.animator.SetDefaultAnimation("Stare", 1f, true);
                foxo.AudMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("bettergrades"));
                foxo.StartCoroutine(cutsceneFloor2());
            }
            else if (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 1 && !foxo.IsBadPhase1)
            {
                foxo.animator.SetDefaultAnimation("Happy", 1f);
                foxo.spriteBase.transform.localPosition += Vector3.down * 10f;
            }
            foxo.Navigator.SetSpeed(0f);
            ChangeNavigationState(new NavigationState_DoNothing(foxo, 127));

            if (!(foxo.IsBadPhase1 || foxo.IsBadPhase2 || foxo.IsBadPhase3 || infbad || infmessed || infwrath || (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel > 0)))
                if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.seasons"))
                    foxo.ReplacementMusic = SeasonCycleStuff.CheckIfNight() ? Foxo.foxoAssets.Get<SoundObject>("schoolnight") : Foxo.foxoAssets.Get<SoundObject>("school");
                else
                    foxo.ReplacementMusic = Foxo.foxoAssets.Get<SoundObject>("school");
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
            CoreGameManager.Instance.audMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("messedup"));
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

        private bool scaryTime = false;
        private RoomController playerCurrentRoom;
        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            base.NotebookCollected(currentNotebooks, maxNotebooks);
            scaryTime = true;
            bool isBad = foxo.IsBadPhase1 || foxo.IsBadEndlessFloor(Mathf.RoundToInt(BaseGameManager.Instance.CurrentLevel + 1 / 2f), 3) ||
                (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 1);
            if (foxo.IsHelping() || (TeacherManager.Instance.SpoopModeActivated && !isBad))
            {
                foxo.ActivateSpoopMode();
                foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
                CoreGameManager.Instance.audMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("ding"));
            }
            else if (isBad)
                foxo.behaviorStateMachine.ChangeState(new Foxo_Scary(foxo));
            else
            {
                foxo.TeleportToNearestDoor();
                playerCurrentRoom = foxo.ec.Players[0].plm.Entity.CurrentRoom;
                foxo.animator.SetDefaultAnimation("Stare", 1f, true);
            }
        }

        public override void PlayerSighted(PlayerManager player)
        {
            if (!foxo.IsHelping() && scaryTime)
                foxo.behaviorStateMachine.ChangeState(new Foxo_Scary(foxo));
        }

        public override void Update()
        {
            base.Update();
            if (playerCurrentRoom != null && !foxo.IsHelping() && scaryTime && foxo.ec.Players[0].plm.Entity.CurrentRoom != playerCurrentRoom)
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
            bool intenseMode = (!TeacherPlugin.IsEndlessFloorsLoaded() && PlayerFileManager.Instance.lifeMode == LifeMode.Intense && BaseGameManager.Instance.CurrentLevel == 1);
            if (!(intenseMode || foxo.IsBadPhase1 || foxo.IsBadPhase2 || foxo.IsBadPhase3 || infbad || infmessed || infwrath))
            {
                foxo.animator.SetDefaultAnimation("Stare", 1f, true);

                // Stop playing songs and be scary for once!!
                MusicManager.Instance.StopMidi();
                foxo.ec.lightMode = LightMode.Greatest;
                foxo.ec.standardDarkLevel = Color.black;
                CoreGameManager.Instance.musicMan.FlushQueue(true);
                CoreGameManager.Instance.musicMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("fear"));
                foxo.ec.FlickerLights(true);

                foxo.StartCoroutine(GetMad());
            }
            else if (intenseMode)
            {
                foxo.ActivateSpoopMode();
                foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
            }
            else if ((foxo.IsBadPhase1 || infbad) && !foxo.IsBadPhase2 && !infmessed)
            {
                Cell cell = foxo.ec.RandomCell(false, false, true);
                while ((cell.CenterWorldPosition - foxo.ec.Players[0].transform.position).magnitude < 111f)
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
            yield return new WaitForSecondsNPCTimescale(foxo, 13f + StickerManager.Instance.StickerValue(Sticker.BaldiCountdown));
            foxo.ec.FlickerLights(false);
            AudioManager aud = _audMan.GetValue(foxo.ec) as AudioManager;
            aud.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("ding"));
            foxo.ActivateSpoopMode();
            foxo.behaviorStateMachine.ChangeState(new Foxo_Chase(foxo));
            yield break;
        }
    }
    public class Foxo_Chase : Foxo_StateBase
    {
        protected float delayTimer;
        public Foxo_Chase(Foxo foxo) : base(foxo) { }
        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
            if (isValid && foxo.IsTouchingPlayer(other))
                foxo.CaughtPlayer(foxo.target);
        }
        public override void GoodMathMachineAnswer(float timer)
        {
            if (foxo.forceWrath || foxo.behaviorStateMachine.currentState.GetType().Equals(typeof(Foxo_Wrath))) return;
            foxo.behaviorStateMachine.ChangeState(new Foxo_Praise(foxo, this, timer));
        }
        public override void Enter()
        {
            base.Enter();
            foxo.animator.SetDefaultAnimation("SlapIdle", 1f, true);
            delayTimer = foxo.Delay;
            foxo.ResetSlapDistance();
        }
        private static FieldInfo _extraAnger = AccessTools.DeclaredField(typeof(Baldi), "extraAnger");
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
                var extraAnger = (float)_extraAnger.GetValue(foxo);
                if (extraAnger > 0 && GetType().Equals(typeof(Foxo_Chase)))
                    _extraAnger.SetValue(foxo, extraAnger - 1f);

                //foxo.ec.FindPath(foxo.ec.CellFromPosition(foxo.transform.position), foxo.ec.CellFromPosition(foxo.Navigator.CurrentDestination), PathType.Nav, out List<Cell> paths, out bool suc);
                // This is not fun...
                if (foxo.Navigator.Entity.CurrentRoom?.category == RoomCategory.Special && currentNavigationState.GetType().Equals(typeof(NavigationState_TargetPlayer)))
                {
                    Cell newcell = foxo.ec.RandomCell(false, true, true);
                    while (newcell.room.category != RoomCategory.Hall || Vector3.Distance(newcell.CenterWorldPosition, foxo.target.transform.position) < 150f)
                        newcell = foxo.ec.RandomCell(false, true, true);
                    foxo.Navigator.Entity.Teleport(newcell.CenterWorldPosition);
                    foxo.AudMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("teleport"));
                }
                // Foxo always know where the player is, except in special rooms
                if (!((foxo.target?.GetComponent<PlayerEntity>()?.CurrentRoom != null && foxo.target.GetComponent<PlayerEntity>().CurrentRoom?.category == RoomCategory.Special)
                    /*|| (suc && paths.Exists(x => x.room.category == RoomCategory.Special))*/))
                {
                    if (!currentNavigationState.GetType().Equals(typeof(NavigationState_TargetPlayer)))
                        ChangeNavigationState(new NavigationState_TargetPlayer(foxo, 0, foxo.target.transform.position));
                    currentNavigationState.UpdatePosition(foxo.target.transform.position);
                }
                else if (!currentNavigationState.GetType().Equals(typeof(NavigationState_WanderRandom)) /*|| (suc && paths.Exists(x => x.room.category == RoomCategory.Special))*/)
                {
                    //if (suc && paths.Exists(x => x.room.category == RoomCategory.Special)) foxo.Navigator.ClearCurrentDirs();
                    ChangeNavigationState(new NavigationState_WanderRandom(foxo, 0));
                }

                foxo.Slap();
                ActivateSlapAnimation();
                delayTimer = foxo.Delay;
                DestinationEmpty();
            }
        }


        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            var near = 15f;
            BaldiInteraction nearestInter = null;
            foreach (var interaction in Physics.OverlapSphere(foxo.transform.position, 5f, 2113541, QueryTriggerInteraction.Ignore))
            {
                var distance = (foxo.transform.position - interaction.transform.position).magnitude;
                if (near <= distance && interaction.GetComponent<BaldiInteraction>() != null)
                {
                    nearestInter = interaction.GetComponent<BaldiInteraction>();
                    near = distance;
                }
            }
            if (nearestInter != null && Vector3.Distance(foxo.CurrentDestinationInteraction.transform.position, foxo.transform.position) < 25f && foxo.CurrentDestinationInteraction.Check(me: foxo))
            {
                foxo.CurrentDestinationInteraction.Trigger(me: foxo);
                foxo.ClearDestinationInteraction();
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
        protected float time;

        public Foxo_Praise(Foxo foxo, TeacherState previousState, float time = 4f) : base(foxo)
        {
            this.previousState = previousState;
            this.time = time;
        }
        public override void Initialize()
        {
            base.Initialize();
            if (!GetType().IsSubclassOf(typeof(Foxo_Praise)))
                foxo.AudMan.QueueAudio(WeightedSelection<SoundObject>.RandomSelection(foxo.correctSounds), true);
        }
        public override void Enter()
        {
            base.Enter();
            if (!GetType().IsSubclassOf(typeof(Foxo_Praise)))
            {
                foxo.animator.SetDefaultAnimation("Happy", 1f);
                foxo.animator.Play("Happy", 1f);
            }
        }
        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0) foxo.behaviorStateMachine.ChangeState(previousState);
        }
    }
    public class Foxo_Locker : Foxo_Praise
    {
        private BaldiInteraction locker;
        public Foxo_Locker(Foxo foxo, TeacherState previousState, float time, BaldiInteraction locker) : base(foxo, previousState, time)
        {
            this.locker = locker;
        }
        public override void Enter()
        {
            base.Enter();
            foxo.Navigator.SetSpeed(0f);
            foxo.Navigator.maxSpeed = 0f;
        }

        public override void Update()
        {
            base.Update();
            if (locker.ShouldBeCancelled())
                Exit();
        }

        public override void Exit()
        {
            base.Exit();
            locker.Payload(baldi: foxo);
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
                foxo.AudMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("wrathscream"));
                foxo.animator.SetDefaultAnimation("WrathSprayed", 1f);
                foxo.animator.Play("WrathSprayed", 1f);
                return;
            }
            foxo.AudMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("scream"));
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
        private static readonly FieldInfo _ec = AccessTools.DeclaredField(typeof(LanternMode), "ec");

        public override void Initialize()
        {
            base.Initialize();
            var lanternmode = foxo.ec.gameObject.GetOrAddComponent<LanternMode>();
            if ((EnvironmentController)_ec.GetValue(lanternmode) == null) lanternmode.Initialize(foxo.ec);
            lanternmode.AddSource(foxo.ec.Players[0].transform, 5.5f, Color.white);
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
            while ((cell.CenterWorldPosition - foxo.ec.Players[0].transform.position).magnitude < 122f)
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
            if (!isBroken && !foxo.forceWrath)
            {
                foxo.AudMan.PlaySingle(foxo.rulerBreak);
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
                CoreGameManager.Instance.musicMan.QueueAudio(Foxo.foxoAssets.Get<SoundObject>($"wrath{new System.Random(CoreGameManager.Instance.Seed() + CoreGameManager.Instance.sceneObject.levelNo).Next(1, 5)}"), true);
                CoreGameManager.Instance.musicMan.SetLoop(true);
                return;
            }
            AudioManager aud = _audMan.GetValue(foxo.ec) as AudioManager;
            aud.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("wrath"));
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

    internal static class CustomFoxoInteractions
    {
        public static void LockerInteract(BaldiInteraction interaction, Teacher feacher)
        {
            feacher.behaviorStateMachine.ChangeState(new Foxo_Locker((Foxo)feacher, (Foxo_StateBase)feacher.behaviorStateMachine.currentState, 1f, interaction));
            feacher.behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(feacher, 0));
        }

        public static bool LockerCheck(BaldiInteraction interaction, Teacher feacher) => interaction.Check(baldi: feacher);

        public static void LockerPayload(BaldiInteraction interaction, Teacher feacher) => interaction.Payload(baldi: feacher);
    }
}
