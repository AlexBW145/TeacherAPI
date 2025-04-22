using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeacherAPI;
using UnityEngine;

namespace TeacherExtension.Viktor
{
    public class Viktor : Teacher
    {
        public bool AllNotebooksPrank { get; internal set; }
        public bool isQuiet => BaseGameManager.Instance.FoundNotebooks >= Mathf.RoundToInt(ec.notebookTotal / 2) && ec.notebookTotal >= 6;
        internal ViktorTilePollutionManager PollutionManager { get; private set; }
        private bool FirstJacketDirty;

        public AudioManager audMan;
        public override TeacherState GetAngryState() => new Viktor_Chase(this);

        public override TeacherState GetHappyState() => new Viktor_Subsitute(this);

        public override string GetNotebooksText(string amount) => $"{amount} Viktor's Math Notebooks";
        public override WeightedTeacherNotebook GetTeacherNotebookWeight() => new WeightedTeacherNotebook(this)
            .Weight(100)
            .CollectSound(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/NotebookJingle"))
            .Sprite(ViktorPlugin.viktorAssets.Get<Sprite>("Notebook"));

        internal CustomBaldicator Viktorcator { get; private set; }

        public override void Initialize()
        {
            AllNotebooksPrank = ec.notebookTotal < 9;
            base.Initialize();
            Viktorcator = CustomBaldicator.CreateBaldicator();
            Viktorcator.SetHearingAnimation(new CustomAnimation<Sprite>([ViktorPlugin.viktorAssets.Get<Sprite>("ViktorSubsitute")], 0.1f));
            Viktorcator.AddAnimation("Coming", new CustomAnimation<Sprite>([ViktorPlugin.viktorAssets.Get<Sprite>("ViktorEvil")], 0.3f));
            Viktorcator.AddAnimation("ForLater", new CustomAnimation<Sprite>([ViktorPlugin.viktorAssets.Get<Sprite>("ViktorSubsitute")], 0.3f));
            PollutionManager = ec.gameObject.GetOrAddComponent<ViktorTilePollutionManager>();
            Navigator.SetSpeed(0f);
            Navigator.maxSpeed = 0f;
            caughtOffset += Vector3.up;
            audMan.volumeModifier = 2f;
            AddLoseSound(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/Jumpscare"), 9);
        }

        public override void Slap()
        {
            //StopCoroutine("StopDelay");
            if (isQuiet)
                audMan.volumeModifier = 0.5f;
            slapTotal = 0f;
            slapDistance = nextSlapDistance;
            nextSlapDistance = 0f;
            audMan.PlaySingle(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/Walk"));
            Navigator.SetSpeed(slapDistance / (Delay * (MovementPortion * 3f)));
            //StartCoroutine(StopDelay());
        }

        public new void Hear(GameObject source, Vector3 position, int value, bool indicator)
        {
            var currentSoundVal = (int)AccessTools.Field(typeof(Baldi), "currentSoundVal").GetValue(this);
            if (value >= currentSoundVal && indicator)
                for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
                    Viktorcator.ActivateBaldicator("Coming");
            else if (indicator)
                for (int j = 0; j < CoreGameManager.Instance.setPlayers; j++)
                    Viktorcator.ActivateBaldicator("ForLater");
            base.Hear(null, position, value, false);
        }

        [Obsolete("Part of the old version", true)]
        private IEnumerator StopDelay()
        {
            float time = 0.7f;
            while (time > 0f)
            {
                time -= Time.deltaTime * ec.NpcTimeScale;
                yield return null;
            }
            Navigator.SetSpeed(0f);
            Navigator.maxSpeed = 0f;
            yield break;
        }

        public override float DistanceCheck(float val)
        {
            if (behaviorStateMachine.CurrentState is Viktor_Jacket)
                return val;
            return base.DistanceCheck(val);
        }

        internal void GetJacketDirty()
        {
            audMan.volumeModifier = 2f;
            if (!FirstJacketDirty)
            {
                FirstJacketDirty = true;
                audMan.PlaySingle(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/DirtyJacketFirst"));
            }
            else
            {
                SoundObject[] randomselect = new SoundObject[]
                {
                    ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/DirtyJacket1"),
                    ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/DirtyJacket2"),
                    ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/DirtyJacket3"),
                    ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/DirtyJacket4")
                };
                audMan.PlayRandomAudio(randomselect);
            }
        }
    }

    public class Viktor_StateBase : TeacherState
    {
        protected Viktor viktor;
        public Viktor_StateBase(Viktor viktor) : base (viktor)
        {
            this.viktor = viktor;
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
            viktor.Hear(source, position, value, true);
        }

        public virtual void ThePrank() // I'm not even using Viktor's notebooks.
        {
            if (BaseGameManager.Instance.FoundNotebooks == Mathf.RoundToInt(viktor.ec.notebookTotal / 2) && viktor.isQuiet && !viktor.IsHelping())
                CoreGameManager.Instance.GetHud(0).BaldiTv.Speak(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/HalfNotebooks"));
            if (BaseGameManager.Instance.FoundNotebooks >= viktor.ec.notebookTotal)
            {
                if (!viktor.IsHelping() && !viktor.AllNotebooksPrank)
                {
                    var activities = viktor.ec.notebooks.Where(act => act.activity.room != viktor.ec.Players[0].plm.Entity.CurrentRoom).ToList();
                    ListExtensions.ControlledShuffle(activities, new System.Random(CoreGameManager.Instance.Seed()));
                    activities.First().activity.StartResetTimer(0);
                    viktor.ec.notebookTotal++;
                    viktor.AllNotebooksPrank = true;
                    CoreGameManager.Instance.GetHud(0).BaldiTv.Speak(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/LastNotebook"));
                    BaseGameManager.Instance.CollectNotebooks(0);
                }
            }
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            if (viktor.IsHelping() && currentNotebooks >= maxNotebooks && viktor.behaviorStateMachine.CurrentState is not Viktor_Subsitute)
                viktor.behaviorStateMachine.ChangeState(new Viktor_Subsitute(viktor) { veryHappy = true });
        }

        public override void DestinationEmpty()
        {
            viktor.UpdateSoundTarget();
        }

        public override void PlayerInSight(PlayerManager player)
        {
            viktor.ClearSoundLocations();
            viktor.Hear(null, player.transform.position, 127, false);
        }
    }

    public class Viktor_SubState : Viktor_StateBase
    {
        protected Viktor_StateBase prevState;
        public Viktor_SubState(Viktor viktor, Viktor_StateBase state) : base (viktor)
        {
            prevState = state;
        }
    }

    public class Viktor_Subsitute : Viktor_StateBase
    {
        public Viktor_Subsitute(Viktor viktor) : base(viktor) { }
        public override void Enter()
        {
            base.Enter();
            viktor.spriteRenderer[0].sprite = ViktorPlugin.viktorAssets.Get<Sprite>("ViktorSubsitute");
            viktor.ReplacementMusic = ViktorPlugin.viktorAssets.Get<SoundObject>("Music/MathLevel");
            viktor.audMan.PlaySingle(!veryHappy ? ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/Intro") : ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/Praise"));
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            base.NotebookCollected(currentNotebooks, maxNotebooks);
            if (veryHappy) return;
            viktor.audMan.FlushQueue(true);
            viktor.audMan.PlaySingle(ViktorPlugin.viktorAssets.Get<SoundObject>("Viktor/Triggered"));
            viktor.ActivateSpoopMode();
            viktor.behaviorStateMachine.ChangeState(new Viktor_Chase(viktor));
        }
        internal bool veryHappy;
        public override void DestinationEmpty()
        { 
        }
        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }
        public override void PlayerInSight(PlayerManager player)
        {
        }
    }

    public class Viktor_Chase : Viktor_StateBase
    {
        protected float delayTimer;
        public Viktor_Chase(Viktor viktor) : base(viktor) { }
        public override void OnStateTriggerStay(Collider other)
        {
            if (viktor.IsTouchingPlayer(other))
                viktor.CaughtPlayer(other.GetComponent<PlayerManager>());
        }
        public override void Update()
        {
            base.Update();
            viktor.UpdateSlapDistance();
            if (delayTimer <= 0f)
            {
                viktor.Slap();
                delayTimer = viktor.Delay * 2f;
            }
            else if (delayTimer > 0f)
                delayTimer -= Time.deltaTime * viktor.ec.NpcTimeScale;
            IntVector2 gridPosition = IntVector2.GetGridPosition(viktor.transform.position);
            Cell cell = this.viktor.ec.CellFromPosition(gridPosition);
            bool flag2 = cell != null && this.viktor.PollutionManager.IsCellPolluted(cell);
            if (flag2)
            {
                this.viktor.StopCoroutine("StopDelay");
                this.viktor.behaviorStateMachine.ChangeState(new Viktor_Jacket(viktor, this));
            }
        }
        /*public override void GoodMathMachineAnswer()
        {
            viktor.behaviorStateMachine.ChangeState(new Viktor_Praise(viktor, this));
        }*/

        public override void Enter()
        {
            base.Enter();
            viktor.spriteRenderer[0].sprite = ViktorPlugin.viktorAssets.Get<Sprite>("ViktorEvil");
            delayTimer = viktor.Delay;
            viktor.ResetSlapDistance();
        }

        public override void Initialize()
        {
            base.Initialize();
            viktor.ClearSoundLocations();
            foreach (var player in viktor.ec.Players)
                if (player.plm.Entity?.CurrentRoom?.category == RoomCategory.Class)
                {
                    viktor.Hear(null, player.transform.position, 127, false);
                    return;
                }
            viktor.UpdateSoundTarget();
        }
    }

    public class Viktor_Jacket : Viktor_SubState
    {
        public Viktor_Jacket(Viktor viktor, Viktor_StateBase prevState) : base(viktor, prevState) { }
        public override void Enter()
        {
            base.Enter();
            viktor.Navigator.ClearDestination();
            viktor.Navigator.SetSpeed(0f);
            viktor.Navigator.maxSpeed = 0f;
            viktor.GetJacketDirty();
            GottaSweep gottaSweep = null;
            Vector3 loc = viktor.transform.position;
            foreach (NPC npc in viktor.ec.Npcs)
                if (npc.Character == Character.Sweep || npc is GottaSweep)
                {
                    gottaSweep = npc as GottaSweep;
                    loc = gottaSweep.home;
                    break;
                }
            if (gottaSweep == null && viktor.ec.offices.Count > 0)
                loc = viktor.ec.offices[UnityEngine.Random.Range(0, viktor.ec.offices.Count)].RandomEntitySafeCellNoGarbage().TileTransform.position;
            ChangeNavigationState(new NavigationState_TargetPosition(viktor, 127, loc));
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }

        public override void Update()
        {
            if (!viktor.audMan.AnyAudioIsPlaying)
            {
                viktor.Navigator.SetSpeed(20f); viktor.Navigator.maxSpeed = 20f;
            }
        }

        public override void DestinationEmpty()
        {
            if (CurrentNavigationState is NavigationState_TargetPosition && viktor.Navigator.speed != 0)
                viktor.behaviorStateMachine.ChangeState(prevState);
        }

        public override void PlayerInSight(PlayerManager player)
        {
        }
    }
}
