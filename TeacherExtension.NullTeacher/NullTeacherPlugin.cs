using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using TeacherAPI;
using UnityEngine;
using static BepInEx.BepInDependency;

namespace NullTeacher
{
    [BepInPlugin("alexbw145.baldiplus.teacherextension.null", "Null Teacher for MoreTeachers", "1.0.5.0")]
    [BepInDependency("alexbw145.baldiplus.teacherapi", DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", DependencyFlags.HardDependency)]
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
            LoadingEvents.RegisterOnAssetsLoaded(Info, OnassetsLoaded, false);
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
                .AddMetaFlag(NPCFlags.CanHear)
                .SetMinMaxAudioDistance(0, 1000)
                .SetMetaTags(new string[] { "teacher" })
                .Build();
            teacher.ReflectionSetVariable("audMan", teacher.GetComponent<AudioManager>());

            CustomSpriteAnimator animator = teacher.gameObject.AddComponent<CustomSpriteAnimator>();
            animator.spriteRenderer = teacher.spriteRenderer[0];
            teacher.animator = animator;

            TeacherPlugin.RegisterTeacher(teacher);
            NullTeacher = teacher;

            GeneratorManagement.Register(this, GenerationModType.Addend, EditGenerator);
        }

        private void EditGenerator(string floorname, int floornumber, SceneObject ld)
        {
            if (floorname.StartsWith("F") || floorname.StartsWith("END") || floorname.Equals("INF"))
            {
                foreach (var customlevel in ld.GetCustomLevelObjects())
                    customlevel.AddPotentialTeacher(NullTeacher, NullConfiguration.SpawnWeight.Value);
                print($"Added Null to {floorname} (Floor {floornumber})");
            }
        }
    }
}
