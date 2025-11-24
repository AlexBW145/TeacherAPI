using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using TeacherAPI;

namespace NullTeacher
{
    [BepInPlugin("alexbw145.baldiplus.teacherextension.null", "Null Teacher for MoreTeachers", "1.0.5.2")]
    [BepInDependency("alexbw145.baldiplus.teacherapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    public class NullTeacherPlugin : BaseUnityPlugin
    {
        public static NullTeacherPlugin Instance { get; private set; }
        public NullTeacher NullTeacher { get; private set; }

        internal void Awake()
        {
            new Harmony("alexbw145.baldiplus.teacherextension.null").PatchAllConditionals();
            Instance = this;
            TeacherPlugin.RequiresAssetsFolder(this); // Very important, or else people will complain about Beans!
            NullConfiguration.Setup();
            LoadingEvents.RegisterOnAssetsLoaded(Info, OnassetsLoaded, LoadingEventOrder.Pre);
            ModdedSaveGame.AddSaveHandler(Info);
        }

        private void OnassetsLoaded()
        {
            NullAssets.Load();
            var teacher = new NPCBuilder<NullTeacher>(Info)
                .SetName("NullTeacher")
                .SetEnum("NullTeacher")
                .SetPoster(ObjectCreators.CreatePosterObject(new UnityEngine.Texture2D[] { NullAssets.poster }))
                .AddLooker()
                .AddTrigger()
                .SetWanderEnterRooms()
                .AddMetaFlag(NPCFlags.CanHear)
                .SetMinMaxAudioDistance(0, 1000)
                .SetMetaTags(new string[] { "teacher", "faculty" })
                .Build();
            teacher.audMan = teacher.GetComponent<AudioManager>();
            teacher.Navigator.accel = 0f;
            teacher.Navigator.speed = 0f;
            teacher.Navigator.maxSpeed = 0f;
            teacher.Navigator.passableObstacles.Add(PassableObstacle.LockedDoor);
            teacher.spriteRenderer[0].sprite = NullAssets.nullsprite;
            teacher.meBalloons = RandomEventMetaStorage.Instance.Get(RandomEventType.Party).value.ReflectionGetVariable("balloon") as Balloon[];

            TeacherPlugin.RegisterTeacher(teacher);
            NullTeacher = teacher;
            teacher.AddNewBaldiInteraction<HideableLockerBaldiInteraction>(
                (interaction, feacher) => interaction.Check(baldi: feacher),
                (interaction, feacher) => interaction.Payload(baldi: feacher));

            GeneratorManagement.Register(this, GenerationModType.Addend, EditGenerator);
        }

        private void EditGenerator(string floorname, int floornumber, SceneObject ld)
        {
            var meta = ld.GetMeta();
            if (meta == null) return;
            bool flag = false;
            foreach (var customlevel in ld.GetCustomLevelObjects())
            {
                if (customlevel.IsModifiedByMod(Info)) continue;
                customlevel.AddPotentialTeacher(NullTeacher, NullConfiguration.SpawnWeight.Value);
                customlevel.MarkAsModifiedByMod(Info);
            }
            if (flag)
                print($"Added Null to {floorname} (Floor {floornumber})");
        }
    }
}
