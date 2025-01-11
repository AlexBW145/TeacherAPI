using BepInEx.Configuration;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace TeacherAPI
{
    internal class TeacherAPIConfiguration : CustomOptionsCategory
    {
        public static ConfigEntry<bool> EnableBaldi { get; internal set; }
        public static ConfigEntry<bool> EnableCustomWarningScreen  { get; internal set; }
        public static ConfigEntry<bool> DisableAssetsWarningScreen { get; internal set; }
        public static ConfigEntry<bool> DebugMode { get; internal set; }
        public static ConfigEntry<bool> DisableAssistingTeachers { get; internal set; }

        private int i = 0;

        private void AddToggle(ConfigEntry<bool> config, string title, string tooltip)
        {
            var toggle = CreateToggle(title, title,
                config.Value,
                new Vector2(135f, i * -40),
                200
            );
            AddTooltip(toggle, tooltip);
            /*toggle.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().sizeDelta += new Vector2(200, 0);
            toggle.transform.SetParent(transform, false);
            GameObject _hotspot = toggle.ReflectionGetVariable("hotspot") as GameObject;
            _hotspot.GetComponent<StandardMenuButton>().OnPress.AddListener(() => config.Value = toggle.Value);*/
            i++;
        }
        private void AddLabel(string title, Vector2 pos, Vector2 size)
        {
            CreateText(title, title,
                pos,
                MTM101BaldAPI.UI.BaldiFonts.ComicSans24, TextAlignmentOptions.Center,
                size, Color.black);
            /*label.GetComponent<RectTransform>().sizeDelta = size;
            label.transform.SetParent(transform, false);*/
            i++;
        }

        public override void Build()
        {
            AddLabel("Open the config file to change values that requires restarts.", new Vector2(-6, 71), new Vector2(375, 40));
            AddToggle(DebugMode, "Enable Debug Mode", "Some goodies to help for development");
            AddToggle(EnableCustomWarningScreen, "Custom Warning Screen", "Enable the custom Warning Screen text changed by TeacherAPI.");
            AddToggle(DisableAssistingTeachers, "Disable Assisting Teachers", "Completely disables teachers assisting other teachers.");
        }

        internal static void Setup()
        {
            EnableBaldi = TeacherPlugin.Instance.Config.Bind(
                "General",
                "EnableBaldi",
                false,
                "Disables all other teachers and makes Baldi spawn in instead."
            );
            EnableCustomWarningScreen = TeacherPlugin.Instance.Config.Bind(
                "General",
                "EnableCustomWarningScreen",
                true,
                "The Warning Screen text at the start of the game is changed by TeacherAPI, doesn't affect the Warning Screen patch."
            );
            DisableAssistingTeachers = TeacherPlugin.Instance.Config.Bind(
                "General",
                "DisableAssistingTeachers",
                false,
                "Completely disables secondary teachers from appearing."
            );
            DisableAssetsWarningScreen = TeacherPlugin.Instance.Config.Bind(
                "Dangerous",
                "DisableAssetsWarningScreen",
                false,
                "Completely disables every patches related to Warning Screen, will show a error in the console instead if the mod assets are not installed. Please make sure to have the console enabled first."
            );
            DebugMode = TeacherPlugin.Instance.Config.Bind(
                "Developement",
                "DebugMode",
                false,
                "Skips Logo, Warning Screen, NameMenu (on later versions of BB +Dev API). And helps a bit with debugging."
            );
        }
    }
}
