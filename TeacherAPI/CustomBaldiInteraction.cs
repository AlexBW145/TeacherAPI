using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TeacherAPI
{
    public static class CustomBaldiInteraction
    {
        internal static readonly Dictionary<Character, Dictionary<Type, Action<BaldiInteraction, Teacher>>> teacherTriggers = new Dictionary<Character, Dictionary<Type, Action<BaldiInteraction, Teacher>>>();
        internal static readonly Dictionary<Character, Dictionary<Type, Action<BaldiInteraction, Teacher>>> teacherPayloads = new Dictionary<Character, Dictionary<Type, Action<BaldiInteraction, Teacher>>>();
        internal static readonly Dictionary<Character, Dictionary<Type, Func<BaldiInteraction, Teacher, bool>>> teacherCheck = new Dictionary<Character, Dictionary<Type, Func<BaldiInteraction, Teacher, bool>>>();

#if DEBUG
        public static bool Check(this BaldiInteraction interaction, Teacher me)
        {
            TeacherPlugin.Log.LogInfo($"Invoking custom {me.gameObject.name} interaction check for {interaction.GetType().Name}");
            return teacherCheck[me.Character]?[interaction.GetType()]?.Invoke(interaction, me) == true;
        }

        public static void Trigger(this BaldiInteraction interaction, Teacher me) 
        {
            TeacherPlugin.Log.LogInfo($"Invoking custom {me.gameObject.name} interaction trigger for {interaction.GetType().Name}");
            teacherTriggers[me.Character]?[interaction.GetType()]?.Invoke(interaction, me);
        }

        public static void Payload(this BaldiInteraction interaction, Teacher me)
        {
            TeacherPlugin.Log.LogInfo($"Invoking custom {me.gameObject.name} interaction payload for {interaction.GetType().Name}");
            teacherPayloads[me.Character]?[interaction.GetType()]?.Invoke(interaction, me);
        }
#else
        public static bool Check(this BaldiInteraction interaction, Teacher me)
        {
            if (teacherCheck.ContainsKey(me.Character) && teacherCheck[me.Character].ContainsKey(interaction.GetType()))
                return teacherCheck[me.Character][interaction.GetType()]?.Invoke(interaction, me) == true;
            return false;
        }

        public static void Trigger(this BaldiInteraction interaction, Teacher me)
        {
            if (teacherTriggers.ContainsKey(me.Character) && teacherTriggers[me.Character].ContainsKey(interaction.GetType()))
                teacherTriggers[me.Character][interaction.GetType()]?.Invoke(interaction, me);
        }

        public static void Payload(this BaldiInteraction interaction, Teacher me)
        {
            if (teacherPayloads.ContainsKey(me.Character) && teacherPayloads[me.Character].ContainsKey(interaction.GetType()))
                teacherPayloads[me.Character][interaction.GetType()]?.Invoke(interaction, me);
        }
#endif
    }
}
