using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TeacherAPI
{
    public static class CustomBaldiInteraction
    {
        public delegate void TeacherInteractionTrigger(BaldiInteraction interaction, Teacher teacher);
        public delegate void TeacherInteractionPayload(BaldiInteraction interaction, Teacher teacher);
        public delegate bool TeacherInteractionCheck(BaldiInteraction interaction, Teacher teacher);

        internal static readonly Dictionary<Character, Dictionary<Type, TeacherInteractionTrigger>> teacherTriggers = new Dictionary<Character, Dictionary<Type, TeacherInteractionTrigger>>();
        internal static readonly Dictionary<Character, Dictionary<Type, TeacherInteractionPayload>> teacherPayloads = new Dictionary<Character, Dictionary<Type, TeacherInteractionPayload>>();
        internal static readonly Dictionary<Character, Dictionary<Type, TeacherInteractionCheck>> teacherCheck = new Dictionary<Character, Dictionary<Type, TeacherInteractionCheck>>();

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
    }
}
