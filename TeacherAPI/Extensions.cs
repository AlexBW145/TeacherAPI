using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections.Generic;
using System.Linq;
using TeacherAPI.utils;
using UnityEngine;

namespace TeacherAPI
{
    public static class Extensions
    {
        /// <summary>
        /// Get every teachers in this EnvironmentController.
        /// </summary>
        /// <param name="ec"></param>
        /// <returns></returns>
        public static Teacher[] GetTeachers(this EnvironmentController ec)
        {
            return (from npc in ec.Npcs where npc.IsTeacher() select (Teacher)npc).ToArray();
        }

        /// <summary>
        /// Check if the NPC is registered as a Teacher in TeacherAPI
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static bool IsTeacher(this NPC npc)
        {
            return TeacherPlugin.Instance.whoAreTeachers.ContainsKey(npc.Character);
        }

        /// <summary>
        /// Am I crazy ?
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal static PromiseLike<TeacherState> AsTeacherState(this NpcState state)
        {
            var promise = new PromiseLike<TeacherState>();
            try
            {
                var teacherstate = (TeacherState)state;
                promise.Resolve(teacherstate);
            }
            catch (Exception ex)
            {
                TeacherPlugin.Log.LogWarning($"Handled InvalidCastException : Tried to cast {state} to Teacher State");
                promise.Fail(ex);
            }
            return promise;
        }
        internal static void AsTeacherState(this NpcState state, Action<TeacherState> action)
        {
            state.AsTeacherState().IfSuccess(action);
        }

        /// <summary>
        /// Adds your teacher into the pool of potential teachers of the level. Doesn't affects potentialBaldis.
        /// </summary>
        /// <param name="levelObject"></param>
        /// <param name="teacher">The teacher to be added</param>
        /// <param name="weight">The weight of the teacher for the selection (as a reference, MoreTeachers default teachers have a weight of 100)</param>
        public static void AddPotentialTeacher(this CustomLevelObject levelObject, Teacher teacher, int weight)
        {
            var teacherlist = levelObject.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialTeachers") as List<WeightedSelection<Teacher>>;
                teacherlist.Add(
                new WeightedSelection<Teacher>() { selection = teacher, weight = weight }
            );
            levelObject.SetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialTeachers", teacherlist);
        }

        /// <summary>
        /// Adds your teacher into the pool of potential assisting teachers of the level.
        /// </summary>
        /// <param name="levelObject"></param>
        /// <param name="teacher">The teacher to be added</param>
        /// <param name="weight">The weight of the teacher for the selection (as a reference, MoreTeachers default teachers have a weight of 100)</param>
        public static void AddPotentialAssistingTeacher(this CustomLevelObject levelObject, Teacher teacher, int weight)
        {
            var assistantList = levelObject.GetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialAssistants") as List<WeightedSelection<Teacher>>;
                assistantList.Add(
                new WeightedSelection<Teacher>() { selection = teacher, weight = weight }
            );
            levelObject.SetCustomModValue(TeacherPlugin.Instance.Info, "TeacherAPI_PotentialTeachers", assistantList);
        }

        public static Sprite ToSprite(this Texture2D tex, float pixelsPerUnit)
        {
            return AssetLoader.SpriteFromTexture2D(tex, pixelsPerUnit);
        }
    }
}
