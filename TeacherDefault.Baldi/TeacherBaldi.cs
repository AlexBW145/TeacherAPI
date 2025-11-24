using HarmonyLib;
using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.Linq;
using System.Reflection;
using TeacherAPI;
using UnityEngine;

namespace TeacherExtension.Baldimore
{
    public class TeacherBaldi : Teacher
    {
        public override TeacherState GetAngryState() => new TeacherBaldi_Chase(this);
        public override TeacherState GetHappyState() => new TeacherBaldi_Happy(this);
        public override TeacherState GetPraiseState(float time) => new TeacherBaldi_Praise(this, ((Baldi_Praise)behaviorStateMachine.currentState).GetPreviousBaldiState(), time);
        public override AssistantPolicy GetAssistantPolicy() => new AssistantPolicy(PossibleAssistantAllowType.Deny)
            .MaxAssistants(BaldiPlugin.EveryAssistantIsHere.Value ? (int)BaseGameManager.Instance?.levelObject?.roomGroup.ToList()?.Find(rm => rm.name == "Class").maxRooms : Mathf.Max(BaseGameManager.Instance.CurrentLevel, CoreGameManager.Instance.sceneObject.levelNo));
        public override WeightedTeacherNotebook GetTeacherNotebookWeight() => new WeightedTeacherNotebook(this)
            .Weight(150);

        [SerializeField] internal SoundObject[] audCountdown;
        [SerializeField] internal SoundObject audHere;
        internal Animator animator
        {
            get => (Animator)_animator.GetValue(this);
            set => _animator.SetValue(this, value);
        }
        internal VolumeAnimator volumeAnimator
        {
            get => (VolumeAnimator)_volumeAnimator.GetValue(this);
            set => _volumeAnimator.SetValue(this, value);
        }
        [SerializeField] internal RuntimeAnimatorController spoopAnimController;
        [SerializeField] internal CustomSpriteRendererAnimator animatorForIntro;
        [SerializeField] internal Sprite introSpr, count, countpeek, countidle;

        internal static readonly FieldInfo
            _animator = AccessTools.DeclaredField(typeof(Baldi), "animator"),
            _volumeAnimator = AccessTools.DeclaredField(typeof(Baldi), "volumeAnimator"),
            _happyBaldiPreMain = AccessTools.DeclaredField(typeof(MainGameManager), "happyBaldiPre"),
            _happyBaldiPreEND = AccessTools.DeclaredField(typeof(EndlessGameManager), "happyBaldiPre"),
            _audIntro = AccessTools.DeclaredField(typeof(HappyBaldi), "audIntro");

        public override void Initialize()
        {
            base.Initialize();
            animatorForIntro.ec = ec;
            navigator.Entity.SetHeight(5.5f);
            volumeAnimator.enabled = false;
            animator.enabled = false;
        }

        public override void Slap()
        {
            volumeAnimator.enabled = false;
            base.Slap();
        }

        protected override void OnRulerBroken()
        {
            behaviorStateMachine.ChangeState(new TeacherBaldi_ChaseBroken(this));
            BreakRuler();
        }
        protected override void OnRulerRestored()
        {
            behaviorStateMachine.ChangeState(new TeacherBaldi_Chase(this));
            RestoreRuler();
        }
    }

    public class TeacherBaldi_StateBase : TeacherState
    {
        protected TeacherBaldi baldi;
        public TeacherBaldi_StateBase(TeacherBaldi baldi) : base(baldi) { this.baldi = baldi; }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
            base.Hear(source, position, value);
            teacher.Hear(source, position, value, true);
        }

        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);
            teacher.ClearSoundLocations();
            teacher.Hear(null, player.transform.position, 127, false);
        }

        public override void NavigationStateChanged()
        {
            base.NavigationStateChanged();
            teacher.ClearDestinationInteraction();
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            if (baldi.CurrentDestinationInteraction != null && (baldi.CurrentDestinationInteraction.Check(me: baldi) || baldi.CurrentDestinationInteraction.Check(baldi: baldi)))
            {
                if (baldi.CurrentDestinationInteraction.Check(me: baldi))
                    baldi.CurrentDestinationInteraction.Trigger(me: baldi);
                else
                    baldi.CurrentDestinationInteraction.Trigger(baldi: baldi);
                baldi.ClearDestinationInteraction();
            }

            baldi.UpdateSoundTarget();
        }

        protected virtual void ActivateSlapAnimation() => teacher.SlapNormal();

        public override void DoorHit(StandardDoor door)
        {
            if (door.locked)
            {
                door.Unlock();
                door.OpenTimed(5f, makeNoise: false);
            }
            else
            {
                base.DoorHit(door);
            }
        }
    }

    public class TeacherBaldi_Happy : TeacherBaldi_StateBase
    {
        public TeacherBaldi_Happy(TeacherBaldi baldi) : base(baldi) { }
        private bool activated;

        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }

        public override void Enter()
        {
            baldi.spriteRenderer[0].sprite = baldi.introSpr;
            base.Enter();
            baldi.behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(baldi, 127));
        }

        public override void Initialize()
        {
            base.Initialize();
            if (baldi.IsHelping()) return;
            baldi.animatorForIntro.Play("Wavee", 0.5f);
        }

        public override void Update()
        {
            if (baldi.animatorForIntro.enabled && !baldi.IsHelping() && baldi.animatorForIntro.AnimationFrame == 14 && !baldi.AudMan.QueuedAudioIsPlaying)
            {
                SoundObject hello = Resources.FindObjectsOfTypeAll<SoundObject>().Last(snd => snd.name == "BAL_IntroKL2");
                HappyBaldi happb = null;
                if (BaseGameManager.Instance is MainGameManager)
                {
                    var maingame = BaseGameManager.Instance as MainGameManager;
                    happb = TeacherBaldi._happyBaldiPreMain.GetValue(maingame) as HappyBaldi;
                }
                else if (BaseGameManager.Instance is EndlessGameManager)
                {
                    var endless = BaseGameManager.Instance as EndlessGameManager;
                    happb = TeacherBaldi._happyBaldiPreEND.GetValue(endless) as HappyBaldi;
                }
                if (happb != null)
                    hello = TeacherBaldi._audIntro.GetValue(happb) as SoundObject;
                baldi.AudMan.PlaySingle(hello);
            }
            if (!baldi.IsHelping() && !baldi.AudMan.QueuedAudioIsPlaying && Vector3.Distance(teacher.transform.position, teacher.ec.Players[0].transform.position) >= 25f && !activated)
            {
                activated = true;
                teacher.StartCoroutine(SpawnWait());
            }
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            if (baldi.IsHelping())
            {
                baldi.AudMan.QueueAudio(baldi.audHere);
                GoHunt();
            }
        }

        private void GoHunt()
        {
            baldi.animator.enabled = true;
            baldi.animatorForIntro.enabled = false;
            teacher.ActivateSpoopMode();
            baldi.animator.runtimeAnimatorController = baldi.spoopAnimController;
            baldi.behaviorStateMachine.ChangeState(baldi.GetAngryState());
            baldi.ClearSoundLocations();
            ChangeNavigationState(new NavigationState_WanderRandom(baldi, 0));
        }

        private IEnumerator SpawnWait()
        {
            yield return null;
            float time = 1f;
            int count = 9;
            while (baldi.AudMan.QueuedAudioIsPlaying || CoreGameManager.Instance.Paused)
                yield return null;
            baldi.animatorForIntro.enabled = false;

            while (count >= 0)
            {
                baldi.AudMan.QueueAudio(baldi.audCountdown[count]);
                baldi.spriteRenderer[0].sprite = baldi.count;
                while (time > 0f)
                {
                    time -= Time.deltaTime * baldi.ec.NpcTimeScale * 0.5f;
                    if (time <= 0.5f && baldi.spriteRenderer[0].sprite == baldi.count)
                    {
                        if (UnityEngine.Random.value * 100f < 1f)
                            baldi.spriteRenderer[0].sprite = baldi.countpeek;
                        else
                            baldi.spriteRenderer[0].sprite = baldi.countidle;
                    }
                    yield return null;
                }

                count--;
                time = 1f;
            }

            baldi.AudMan.QueueAudio(baldi.audHere);
            baldi.spriteRenderer[0].sprite = baldi.countidle;
            yield return null;
            while (baldi.AudMan.QueuedAudioIsPlaying || CoreGameManager.Instance.Paused)
                yield return null;
            GoHunt();
        }
    }

    public class TeacherBaldi_Chase : TeacherBaldi_StateBase
    {
        protected float delayTimer;
        public TeacherBaldi_Chase(TeacherBaldi baldi) : base(baldi) { }

        public override void Initialize()
        {
            base.Initialize();
            baldi.animator.enabled = true;
            baldi.animatorForIntro.enabled = false;
            baldi.animator.runtimeAnimatorController = baldi.spoopAnimController;
        }
        public override void Enter()
        {
            base.Enter();
            delayTimer = baldi.Delay;
            baldi.ResetSlapDistance();
        }

        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
            base.OnStateTriggerStay(other, isValid);
            if (!baldi.IsTouchingPlayer(other) || !isValid)
                return;

            PlayerManager component = other.GetComponent<PlayerManager>();
            ItemManager itm = component.itm;
            if (!component.invincible)
            {
                if (itm.Has(Items.Apple))
                {
                    itm.Remove(Items.Apple);
                    baldi.TakeApple();
                }
                else
                {
                    var baldiActualState = new Baldi_Chase(baldi, baldi);
                    baldiActualState.OnStateTriggerStay(other, isValid);
                    //baldi.CaughtPlayer(component);
                }
            }
        }

        public override void Update()
        {
            base.Update();
            baldi.UpdateSlapDistance();
            delayTimer -= Time.deltaTime * npc.TimeScale;
            if (delayTimer <= 0f)
            {
                baldi.Slap();
                ActivateSlapAnimation();
                delayTimer = baldi.Delay;
            }
        }
    }

    public class TeacherBaldi_Praise : TeacherBaldi_StateBase
    {
        private float time;
        protected NpcState previousState;

        public TeacherBaldi_Praise(TeacherBaldi baldi, NpcState previousState, float time)
            : base(baldi)
        {
            this.previousState = previousState;
            this.time = time;
        }

        public override void Initialize()
        {
            base.Initialize();
            baldi.AudMan.QueueAudio(WeightedSelection<SoundObject>.RandomSelection(baldi.correctSounds));
            baldi.volumeAnimator.enabled = true;
        }

        public override void Enter()
        {
            if (!initialized)
                Initialize();
            baldi.PraiseAnimation();
        }

        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0f)
            {
                npc.behaviorStateMachine.ChangeState(previousState);
            }
        }
    }

    public class TeacherBaldi_ChaseBroken : TeacherBaldi_Chase
    {
        private bool broken;
        public TeacherBaldi_ChaseBroken(TeacherBaldi baldi) : base(baldi) { }
        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
        }
        protected override void ActivateSlapAnimation()
        {
            if (!broken)
            {
                teacher.SlapBreak();
                broken = true;
            }
            else
            {
                teacher.SlapBroken();
            }
        }
    }
}
