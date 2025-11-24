using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TeacherAPI;
using UnityEngine;

namespace TeacherAPI
{
    public abstract class Teacher : Baldi
    {

        /// <summary>
        /// Small offset added to the camera during Jumpscare.
        /// </summary>
        [SerializeField] protected Vector3 caughtOffset = Vector3.zero;

        /// <summary>
        /// When enabled, doesn't spawns NPCs when spoopmode activates. Also removes NPC posters from office.
        /// </summary>
        public bool disableNpcs = false;

        public virtual AssistantPolicy GetAssistantPolicy() => new AssistantPolicy(PossibleAssistantAllowType.Deny);

        internal bool HasInitialized;
        private TeacherManager teacherManager;
        public TeacherManager TeacherManager => teacherManager;

        private readonly static FieldInfo
            _slapCurve = AccessTools.DeclaredField(typeof(Baldi), "slapCurve"),
            _speedCurve = AccessTools.DeclaredField(typeof(Baldi), "speedCurve"),
            _breakRuler = AccessTools.DeclaredField(typeof(Baldi), "breakRuler"),
            _restoreRuler = AccessTools.DeclaredField(typeof(Baldi), "restoreRuler"),
            _slap = AccessTools.DeclaredField(typeof(Baldi), "slap"),
            _rulerBreak = AccessTools.DeclaredField(typeof(Baldi), "rulerBreak"),
            _smoothMove = AccessTools.DeclaredField(typeof(Baldi), "smoothMove"),
            _correctSounds = AccessTools.DeclaredField(typeof(Baldi), "correctSounds"),
            _OnBaldiSlap = AccessTools.Field(typeof(Baldi), "OnBaldiSlap"),
            _eventIntro = AccessTools.DeclaredField(typeof(RandomEvent), "eventIntro"),
            _happyBaldiSpriteRenderer = AccessTools.DeclaredField(typeof(HappyBaldi), "sprite"),
            _audMan = AccessTools.DeclaredField(typeof(Baldi), "audMan"),
            _currentDestinationInteraction = AccessTools.DeclaredField(typeof(Baldi), "currentDestinationInteraction"),
            _baldiInteractions = AccessTools.DeclaredField(typeof(Baldi), "baldiInteractions");

        public BaldiInteraction currentDestinationInteraction 
        {
            protected get => CurrentDestinationInteraction;
            set => _currentDestinationInteraction.SetValue(this, value); 
        }
        /// <summary>
        /// If true, the teacher will move regularly instead of using Baldi's slap movement.
        /// </summary>
        public bool smoothMove
        {
            get => (bool)_smoothMove.GetValue(this);
            set => _smoothMove.SetValue(this, value);
        }
        public AudioManager audMan 
        {
            protected get => AudMan;
            set => _audMan.SetValue(this, value); 
        }
        /// <summary>
        /// The slap sound for the teacher, getter exists for purposes like mod compatability.
        /// </summary>
        public SoundObject slap
        {
            get => (SoundObject)_slap.GetValue(this);
            set => _slap.SetValue(this, value);
        }
        /// <summary>
        /// Used for the broken ruler random event.
        /// </summary>
        public SoundObject rulerBreak
        {
            get => (SoundObject)_rulerBreak.GetValue(this);
            set => _rulerBreak.SetValue(this, value);
        }
        /// <summary>
        /// The sounds that the teacher plays when appraising the player.
        /// </summary>
        public WeightedSoundObject[] correctSounds
        {
            get => (WeightedSoundObject[])_correctSounds.GetValue(this);
            set => _correctSounds.SetValue(this, value);
        }
        /// <summary>
        /// Set or check to see if the teacher is about to restore their ruler.
        /// </summary>
        public bool restoreRuler
        {
            get => (bool)_restoreRuler.GetValue(this);
            protected set => _restoreRuler.SetValue(this, value);
        }
        /// <summary>
        /// Set or check to see if the teacher has their ruler about to be broken.
        /// </summary>
        public bool breakRuler
        {
            get => (bool)_breakRuler.GetValue(this);
            protected set => _breakRuler.SetValue(this, value);
        }

        // Overrides
        public override void Initialize()
        {
            navigator.Initialize(ec);
            /*
             * To: typeof(Baldi_Chase)
             * You stink, you used ResetSprite() on your initialization!!
             * From: AlexBW145
             */

            // Cancel state machine of bladder
            behaviorStateMachine.ChangeState(new TeacherState(this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
            navigator.Entity.SetInteractionState(false);

            var ld = BaseGameManager.Instance.levelObject as CustomLevelGenerationParameters;
            var baseBaldi = ld.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_OriginalBaldi") as Baldi;
            if (baseBaldi == null)
            {
                TeacherPlugin.Log.LogWarning($"Base Baldi does not exist in {CoreGameManager.Instance.sceneObject.levelTitle}! Despawning {gameObject.name}!");
                Despawn();
                return;
            }
            TeacherPlugin.Log.LogInfo($"Using {baseBaldi.gameObject.name} as base Baldi.");
            _slapCurve.SetValue(this, _slapCurve.GetValue(baseBaldi));
            _speedCurve.SetValue(this, _speedCurve.GetValue(baseBaldi));
            if (rulerBreak == null)
                rulerBreak = (SoundObject)_rulerBreak.GetValue(baseBaldi);

            baseSpeed = baseBaldi.baseSpeed;
            baseAnger = baseBaldi.baseAnger;

            speedMultiplier = baseBaldi.speedMultiplier;
            slapSpeedScale = baseBaldi.slapSpeedScale;
            appleTime = baseBaldi.appleTime;
            extraAngerDrain = baseBaldi.extraAngerDrain;

            teacherManager = TeacherManager.Instance;
            TeacherManager.Instance.spawnedTeachers.Add(this);
        }

        public override void Despawn()
        {
            base.Despawn();
            TeacherManager.Instance.spawnedTeachers.Remove(this);
        }

        public override void CaughtPlayer(PlayerManager player)
        {
            try { base.CaughtPlayer(player); }
            catch (Exception e)
            {
                MTM101BaldiDevAPI.CauseCrash(TeacherPlugin.Instance.Info, e);
            }
            CoreGameManager.Instance.GetCamera(0).offestPos += caughtOffset;
        }

        /// <summary>
        /// The state of the teacher when he goes angry. 
        /// Only used when your Teacher is being spawned during SpoopMode.
        /// This is also used when another Teacher went into SpoopMode.
        /// The state automatically change after Initialize by an integrated HarmonyPatch!!!
        /// </summary>
        /// <returns></returns>
        public abstract TeacherState GetAngryState();

        /// <summary>
        /// The state of your teacher when spawned before SpoopMode.
        /// The state automatically change after Initialize by an integrated HarmonyPatch!!!
        /// </summary>
        /// <returns></returns>
        public abstract TeacherState GetHappyState();
        /// <summary>
        /// The state of your teacher when an activity gets completed without any answers wrong.
        /// Used for a patch towards other things that may will praise the teacher in any way.
        /// </summary>
        /// <returns></returns>
        public abstract TeacherState GetPraiseState(float time);

        // Ruler related stuff
        protected virtual void OnRulerBroken()
        {

        }
        protected virtual void OnRulerRestored()
        {

        }
        public override void Slap()
        {
            ((BaldiSlapDelegate)_OnBaldiSlap.GetValue(null)).Invoke(this);
            slapTotal = 0f;
            slapDistance = nextSlapDistance;
            nextSlapDistance = 0f;
            navigator.SetSpeed(slapDistance / (Delay * MovementPortion));
            if (breakRuler)
            {
                OnRulerBroken();
                breakRuler = false;
                return;
            }
            if (restoreRuler)
            {
                OnRulerRestored();
                restoreRuler = false;
                return;
            }
        }

        public new void ResetSprite()
        {

        }

        // Methods to customize Teacher
        /// <summary>
        /// Replace the event text of the initialized event.
        /// </summary>
        /// <typeparam name="RandomEvent"></typeparam>
        /// <param name="text"></param>
        [Obsolete("Since v0.6.0, event text was replaced with the Baldi TV and a SoundObject intro.")]
        public void ReplaceEventText<RandomEvent>(string text) where RandomEvent : global::RandomEvent
        {
            Debug.Log("This is not v0.3.8 or v0.5.2, use `ReplaceEventText<RandomEvent>(SoundObject aud)` instead!");
        }
        /// <summary>
        /// Replace the event audio of the initialized event.
        /// </summary>
        /// <typeparam name="RandomEvent"></typeparam>
        /// <param name="aud"></param>
        public void ReplaceEventText<RandomEvent>(SoundObject aud) where RandomEvent : global::RandomEvent
        {
            if (TeacherManager.MainTeacherPrefab.Character != Character) return;
            var events = ec.gameObject.GetComponentsInChildren<RandomEvent>();
            foreach (var randomEvent in events)
                _eventIntro.SetValue(randomEvent, aud);
        }

        /// <summary>
        /// Check if the teacher is touching the player.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsTouchingPlayer(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                looker.Raycast(other.transform, Vector3.Magnitude(transform.position - other.transform.position), out bool targetSighted);
                if (targetSighted)
                {
                    PlayerManager plr = other.GetComponent<PlayerManager>();
                    if (!plr.invincible)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Easily add a sound to the loseSounds of Teacher
        /// When the player is Caught, it will choose one randomly defined by the weight.
        /// You can also define loseSounds directly if you prefer not using this method.
        /// Either way, a loseSound must be defined.
        /// </summary>
        /// <param name="snd"></param>
        /// <param name="weight"></param>
        public void AddLoseSound(SoundObject snd, int weight)
        {
            loseSounds = loseSounds.AddToArray(new WeightedSoundObject() { selection = snd, weight = weight });
        }

        /// <summary>
        /// Starts the game fr, calling ActivateSpoopMode during Free Run will despawn your teacher!
        /// Can only be called once per level no matter how much teacher there are.
        /// </summary>
        public void ActivateSpoopMode()
        {
            navigator.Entity.SetInteractionState(true);
            if (TeacherManager.Instance.SpoopModeActivated)
            {
                if (CoreGameManager.Instance.currentMode == Mode.Free)
                    Despawn();
                return;
            }

            // For which who have spawned the custom teacher after Baldi
            var happyBaldi = ec.GetComponentInChildren<HappyBaldi>();
            if (happyBaldi != null)
            {
                var spr = _happyBaldiSpriteRenderer.GetValue(happyBaldi) as SpriteRenderer;
                spr.enabled = false;
            }

            TeacherManager.Instance.SpoopModeActivated = true;
            MusicManager.Instance.StopMidi();
            CoreGameManager.Instance.musicMan.FlushQueue(true);
            BaseGameManager.Instance.BeginSpoopMode();
            if (!teacherManager.SpawnedMainTeacher.disableNpcs)
                ec.SpawnNPCs();
            if (CoreGameManager.Instance.currentMode == Mode.Main)
            {
                // Teacher is already in HappyBaldi position, do nothing.
            }
            else if (CoreGameManager.Instance.currentMode == Mode.Free)
            {
                Despawn();
            }
            ec.StartEventTimers();
            /*foreach (var notebook in ec.notebooks)
            {
                var teacherNotebook = notebook.gameObject.GetComponent<TeacherNotebook>();
                if (TeacherManager.MainTeacherPrefab.Character != teacherNotebook.character)
                {
                    notebook.Hide(false);
                }
            }*/
        }

        /// <summary>
        /// The flavor text for this teacher. 
        /// </summary>
        /// <param name="amount">The amount of notebook such as $"{current}/{max}", or just current in Endless</param>
        /// <returns>The text that shows up on the top left of the screen</returns>
        public virtual string GetNotebooksText(string amount) => $"{amount} {name.Replace("(Clone)", "")} Notebooks";
        public virtual WeightedTeacherNotebook GetTeacherNotebookWeight() => new WeightedTeacherNotebook(this);
        public bool IsHelping() => TeacherManager.MainTeacherPrefab.Character != Character;

        /// <summary>
        /// If set to <see cref="string"/>, then that selected MIDI music will play or not (if string is set to "mute"). If set to <see cref="SoundObject"/>, then that audio clip will play. Leave null for schoolhouse music to play.
        /// </summary>
        public object ReplacementMusic;

        [Obsolete("Please use `Teacher.ReplacementMusic` instead.", true)]
        public void ReplaceMusic(SoundObject snd)
        {
            StartCoroutine(ReplaceMusicDelay(snd));
        }
        [Obsolete("Please use `Teacher.ReplacementMusic` instead.", true)]
        public void ReplaceMusic()
        {
            StartCoroutine(ReplaceMusicDelay());
        }
        [Obsolete("Please use `Teacher.ReplacementMusic` instead.", true)]
        private IEnumerator ReplaceMusicDelay(SoundObject snd = null)
        {
            if (IsHelping())
            {
                yield break;
            }
            // Because the midi isn't playing immediatlely obviously very ugly hack pls help me
            MusicManager.Instance.MidiPlayer.MPTK_Volume = 0;
            yield return new WaitForSeconds(0.05f);
            MusicManager.Instance.StopMidi();
            if (snd) { // May conflict with The Thinker character from Playable Characters mod.
                CoreGameManager.Instance.musicMan.QueueAudio(snd, true);
                CoreGameManager.Instance.musicMan.SetLoop(true);
            }
            yield return new WaitForSeconds(0.25f);
            MusicManager.Instance.MidiPlayer.MPTK_Volume = 1;
            yield break;
        }
    }
}

[Serializable]
public class WeightedTeacher : WeightedSelection<Teacher>
{
    public static List<WeightedSelection<Teacher>> Convert(List<WeightedTeacher> list)
    {
        List<WeightedSelection<Teacher>> list2 = new List<WeightedSelection<Teacher>>();
        foreach (WeightedTeacher item in list)
        {
            list2.Add(item);
        }

        return list2;
    }
}