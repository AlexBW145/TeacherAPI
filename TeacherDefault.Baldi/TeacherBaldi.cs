using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeacherAPI;
using UnityEngine;

namespace TeacherExtension.Baldimore
{
    public class TeacherBaldi : Teacher
    {
        public override TeacherState GetAngryState() => new TeacherBaldi_Chase(this);
        public override TeacherState GetHappyState() => new TeacherBaldi_Happy(this);
        public override AssistantPolicy GetAssistantPolicy() => new AssistantPolicy(PossibleAssistantAllowType.Deny)
            .MaxAssistants(BaldiPlugin.EveryAssistantIsHere.Value ? (int)BaseGameManager.Instance?.levelObject?.roomGroup.ToList()?.Find(rm => rm.name == "Class").maxRooms : Mathf.Max(BaseGameManager.Instance.CurrentLevel, CoreGameManager.Instance.sceneObject.levelNo));
        public override WeightedTeacherNotebook GetTeacherNotebookWeight() => new WeightedTeacherNotebook(this)
            .Weight(150);

        public AudioManager audMan;
        [SerializeField] internal SoundObject[] audCountdown;
        [SerializeField] internal SoundObject audHere;
        //[SerializeField] internal SoundObject slap;
        //[SerializeField] internal SoundObject rulerBreak;
        [SerializeField] internal Animator animator;
        [SerializeField] internal VolumeAnimator volumeAnimator;
        [SerializeField] internal RuntimeAnimatorController spoopAnimController;
        [SerializeField] internal CustomSpriteAnimator animatorForIntro;
        [SerializeField] internal Sprite[] spritesofIntro;
        [SerializeField] internal Sprite count;
        [SerializeField] internal Sprite countpeek;
        [SerializeField] internal Sprite countidle;
        private bool brokeRuler; // I don't know why I didn't make it a public getter...

        public override void Initialize()
        {
            base.Initialize();
            animatorForIntro = gameObject.AddComponent<CustomSpriteAnimator>();
            animatorForIntro.spriteRenderer = spriteRenderer[0];
            animatorForIntro.useUnscaledTime = false;
            animatorForIntro.animations.Add("Wavee", new CustomAnimation<Sprite>(spritesofIntro, 2.5f)); //1.6833f
            navigator.Entity.SetHeight(5.5f);
            volumeAnimator.enabled = false;
            animator.enabled = false;
            behaviorStateMachine.ChangeState(GetHappyState());
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
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
            baldi.spriteRenderer[0].sprite = baldi.spritesofIntro.First();
            base.Enter();
        }

        public override void Initialize()
        {
            base.Initialize();
            if (baldi.IsHelping()) return;
            baldi.animatorForIntro.Play("Wavee", 1f);
        }

        public override void Update()
        {
            if (baldi.animatorForIntro.enabled && !baldi.IsHelping() && baldi.animatorForIntro.currentFrameIndex == 12 && !baldi.audMan.QueuedAudioIsPlaying)
            {
                SoundObject hello = Resources.FindObjectsOfTypeAll<SoundObject>().Last(snd => snd.name == "BAL_IntroKL2");
                if (BaseGameManager.Instance is MainGameManager)
                {
                    var maingame = BaseGameManager.Instance as MainGameManager;
                    var happb = maingame.ReflectionGetVariable("happyBaldiPre") as HappyBaldi;
                    hello = happb.ReflectionGetVariable("audIntro") as SoundObject;
                }
                else if (BaseGameManager.Instance is EndlessGameManager)
                {
                    var endless = BaseGameManager.Instance as EndlessGameManager;
                    var happb = endless.ReflectionGetVariable("happyBaldiPre") as HappyBaldi;
                    hello = happb.ReflectionGetVariable("audIntro") as SoundObject;
                }
                baldi.audMan.PlaySingle(hello);
            }
            if (!baldi.IsHelping() && !baldi.audMan.QueuedAudioIsPlaying && Vector3.Distance(teacher.transform.position, teacher.ec.Players[0].transform.position) >= 25f && !activated)
            {
                activated = true;
                teacher.StartCoroutine(SpawnWait());
            }
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            if (baldi.IsHelping())
            {
                baldi.audMan.QueueAudio(baldi.audHere);
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
            while (baldi.audMan.QueuedAudioIsPlaying || CoreGameManager.Instance.Paused)
                yield return null;
            baldi.animatorForIntro.enabled = false;

            while (count >= 0)
            {
                baldi.audMan.QueueAudio(baldi.audCountdown[count]);
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

            baldi.audMan.QueueAudio(baldi.audHere);
            baldi.spriteRenderer[0].sprite = baldi.countidle;
            yield return null;
            while (baldi.audMan.QueuedAudioIsPlaying || CoreGameManager.Instance.Paused)
                yield return null;
            GoHunt();
        }
    }

    public class TeacherBaldi_Chase : TeacherBaldi_StateBase
    {
        protected float delayTimer;
        public TeacherBaldi_Chase(TeacherBaldi baldi) : base(baldi) { }

        public override void Enter()
        {
            base.Enter();
            delayTimer = baldi.Delay;
            baldi.ResetSlapDistance();
        }

        public override void OnStateTriggerStay(Collider other)
        {
            base.OnStateTriggerStay(other);
            if (!baldi.IsTouchingPlayer(other))
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
                    baldi.CaughtPlayer(component);
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

    public class TeacherBaldi_ChaseBroken : TeacherBaldi_Chase
    {
        private bool broken;
        public TeacherBaldi_ChaseBroken(TeacherBaldi baldi) : base(baldi) { }
        public override void OnStateTriggerStay(Collider other)
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
