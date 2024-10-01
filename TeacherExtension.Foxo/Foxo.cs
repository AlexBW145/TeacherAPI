using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using System.Collections;
using System.Linq;
using System.Reflection;
using TeacherAPI;
using TeacherAPI.utils;
using UnityEngine;

namespace TeacherExtension.Foxo
{
    public class Foxo : Teacher
    {
        public static AssetManager sprites = new AssetManager();
        public static AssetManager audios = new AssetManager();
        public PlayerManager target;
        public bool forceWrath = false;

        // Foxo specifically uses a CustomSpriteAnimator
        public CustomSpriteAnimator animator;

        public static void LoadAssets()
        {
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
                "Wrath",
                TeacherPlugin
                    .TexturesFromMod(FoxoPlugin.Instance, "wrath{0}.png", (1, 3))
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

            // Shortcut functions
            AudioClip Clip(string path) => AssetLoader.AudioClipFromMod(FoxoPlugin.Instance, "audio", path);
            SoundObject NoSubtitle(AudioClip audio, SoundType type)
            {
                var snd = ObjectCreators.CreateSoundObject(audio, "", type, Color.white);
                snd.subtitle = false;
                return snd;
            };

            audios.Add("boing", ObjectCreators.CreateSoundObject(Clip("boing.wav"), "* Boing! *", SoundType.Effect, Color.yellow));
            audios.Add("ding", ObjectCreators.CreateSoundObject(Clip("ding.wav"), "* Ding! *", SoundType.Effect, Color.yellow));
            audios.Add("school", NoSubtitle(Clip("school2.wav"), SoundType.Music));
            audios.Add("hellothere", ObjectCreators.CreateSoundObject(Clip("hellothere.wav"), "Hello there! Welcome to my Fun Schoolhouse.", SoundType.Voice, Color.yellow));
            audios.Add("slap", ObjectCreators.CreateSoundObject(Clip("slap.wav"), "* Slap! *", SoundType.Effect, Color.yellow));
            audios.Add("slap2", ObjectCreators.CreateSoundObject(Clip("slap2.wav"), "...", SoundType.Effect, Color.yellow));
            audios.Add("scare", NoSubtitle(Clip("scare.wav"), SoundType.Effect));
            audios.Add("scream", ObjectCreators.CreateSoundObject(Clip("scream.wav"), "micheal p scream", SoundType.Voice, Color.yellow));
            audios.Add("wrath", NoSubtitle(Clip("wrath.wav"), SoundType.Music));
            audios.Add("fear", NoSubtitle(Clip("fear.wav"), SoundType.Effect));

            audios.Add("praise", new SoundObject[] {
                                ObjectCreators.CreateSoundObject(Clip("praise1.wav"), "Great job, that's great!", SoundType.Voice, Color.yellow),
                                ObjectCreators.CreateSoundObject(Clip("praise2.wav"), "I think you are smarter than me!", SoundType.Voice, Color.yellow),
                        });
            audios.Add("bettergrades", ObjectCreators.CreateSoundObject(Clip("BetterGrades.wav"), "Get some better grades.", SoundType.Voice, Color.yellow));
            audios.Add("messedup", ObjectCreators.CreateSoundObject(Clip("Floor3MessedUp.wav"), "You've messed up...", SoundType.Voice, Color.yellow));
            audios.Add("WrathEventAud", ObjectCreators.CreateSoundObject(Clip("wrath_intro.wav"), "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", SoundType.Voice, Color.yellow));
        }
        public bool IsBadFloor(int num, int deaths)
        {
            return num == BaseGameManager.Instance.CurrentLevel && deaths <= FoxoPlugin.Instance.deathCounter.deaths;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Appearance and sound
            {
                var waveSprites = sprites.Get<Sprite[]>("Wave");
                var slapSprites = sprites.Get<Sprite[]>("Slap");
                var wrathSprites = sprites.Get<Sprite[]>("Wrath");
                animator.animations.Add("Wave", new CustomAnimation<Sprite>(waveSprites, 3f));
                animator.animations.Add("Happy", new CustomAnimation<Sprite>(new Sprite[] { waveSprites[waveSprites.Length - 1] }, 1f));
                animator.animations.Add("Stare", new CustomAnimation<Sprite>((IsBadFloor(1, 2) || IsBadFloor(2, 2)) ? sprites.Get<Sprite[]>("floor2Intro") : new Sprite[] { sprites.Get<Sprite>("Stare") }, 0.02f));

                animator.animations.Add("Slap", new CustomAnimation<Sprite>(slapSprites, 1f));
                animator.animations.Add("SlapIdle", new CustomAnimation<Sprite>(new Sprite[] { slapSprites[slapSprites.Length - 1] }, 1f));

                animator.animations.Add("WrathIdle", new CustomAnimation<Sprite>(new Sprite[] { wrathSprites[0] }, 1f));
                animator.animations.Add("Wrath", new CustomAnimation<Sprite>(wrathSprites.Reverse().ToArray(), 0.3f));
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

        // Ruler related events
        protected override void OnRulerBroken()
        {
            base.OnRulerBroken();
            extraAnger += 3;
            behaviorStateMachine.ChangeState(new Foxo_Wrath(this));
        }
        protected override void OnRulerRestored()
        {
            base.OnRulerRestored();
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
        protected override void VirtualUpdate()
        {
            base.VirtualUpdate();
            if (!(bool)target.plm.ReflectionGetVariable("running"))
                target.plm.AddStamina(target.plm.staminaDrop * 0.8f * Time.deltaTime * target.PlayerTimeScale, true);
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
            if (!((foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || foxo.IsBadFloor(2, 4)) && !foxo.IsHelping()))
            {
                foxo.animator.Play("Wave", 1f);
                foxo.animator.SetDefaultAnimation("Happy", 1f);
                foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("hellothere"));
            }
            else if (foxo.IsBadFloor(2, 4))
            {
                foreach (var light in BaseGameManager.Instance.Ec.lights)
                    light.SetLight(!(light.room.category != RoomCategory.Special));
                Cell cell = foxo.ec.RandomCell(false, false, true);
                while ((cell.CenterWorldPosition - foxo.players[0].transform.position).magnitude < 111f && cell.room.category != RoomCategory.Hall)
                    cell = foxo.ec.RandomCell(false, false, true);
                foxo.Navigator.Entity.Teleport(cell.CenterWorldPosition);
                foxo.StartCoroutine(JustWaitForGameToStart());
            }
            else if ((foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2)) && !foxo.IsBadFloor(2, 4))
            {
                foxo.animator.Play("Stare", 1f);
                foxo.animator.SetDefaultAnimation("Stare", 1f);
                foxo.AudMan.PlaySingle(Foxo.audios.Get<SoundObject>("bettergrades"));
                foxo.StartCoroutine(cutsceneFloor2());
            }
            foxo.Navigator.SetSpeed(0f);
            ChangeNavigationState(new NavigationState_DoNothing(foxo, 32));

            if (!(foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || foxo.IsBadFloor(2, 4)))
                foxo.ReplaceMusic(Foxo.audios.Get<SoundObject>("school"));
            else
                foxo.ReplaceMusic();
        }
        private Vector3 ogPos;

        private IEnumerator cutsceneFloor2()
        {
            yield return new WaitForSecondsEnviromentTimescale(foxo.ec, 0.655f);
            float time = -1.5f;
            while (foxo.spriteBase.transform.localPosition.y > -10f)
            {
                time += 0.1f % (Time.timeScale * foxo.ec.EnvironmentTimeScale);
                foxo.spriteBase.transform.localPosition = Vector3.down * time;
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
            if (!(foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2) || foxo.IsBadFloor(2, 4)))
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
            else if ((foxo.IsBadFloor(1, 2) || foxo.IsBadFloor(2, 2)) && !foxo.IsBadFloor(2, 4))
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
            base.GoodMathMachineAnswer();
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
                {
                    foxo.ReflectionSetVariable("extraAnger", (float)foxo.ReflectionGetVariable("extraAnger") - 1);
                }

                // Foxo always know where the player is, except in special rooms
                if (foxo.target?.GetComponent<PlayerEntity>()?.CurrentRoom != null && foxo.target.GetComponent<PlayerEntity>().CurrentRoom.category != RoomCategory.Special)
                {
                    ChangeNavigationState(new NavigationState_TargetPlayer(foxo, 0, foxo.target.transform.position));
                }

                foxo.Slap();
                ActivateSlapAnimation();
                delayTimer = foxo.Delay;
            }
        }
        protected override void ActivateSlapAnimation() => foxo.SlapNormal();
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

    public class Foxo_WrathHappy : Foxo_StateBase
    {
        public Foxo_WrathHappy(Foxo foxo) : base(foxo) { }

        public override void Enter()
        {
            base.Enter();
            foxo.animator.SetDefaultAnimation("WrathIdle", 1f);
            foxo.Navigator.SetSpeed(0f);
            foxo.spriteBase.SetActive(false);
            ChangeNavigationState(new NavigationState_DoNothing(foxo, 32));
            foxo.ReplaceMusic();
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
        public override void Enter()
        {
            base.Enter();
            foxo.ec.FlickerLights(false);
            if (foxo.forceWrath && !foxo.IsHelping())
            {
                CoreGameManager.Instance.musicMan.QueueAudio(Foxo.audios.Get<SoundObject>("wrath"), true);
                CoreGameManager.Instance.musicMan.SetLoop(true);
                return;
            }
            AudioManager aud = foxo.ec.ReflectionGetVariable("audMan") as AudioManager;
            aud.PlaySingle(Foxo.audios.Get<SoundObject>("wrath"));
        }
        public override void Exit()
        {
            base.Exit();
            foxo.ec.FlickerLights(false);
        }

        private bool isBroken = false;
    }
}
