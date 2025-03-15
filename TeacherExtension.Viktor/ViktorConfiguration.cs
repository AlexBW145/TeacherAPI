using BepInEx.Configuration;

namespace TeacherExtension.Viktor
{
    // It's recommended to at least add Bepinex config to let users tweak your teacher settings.
    // Most of the time, it will be about changing weights.
    public class ViktorConfiguration
    {
        public static ConfigEntry<int> Weight { get; internal set; }

        /// <summary>
        /// Triggered when launching the mod, mostly to setup BepInEx config bindings.
        /// </summary>
        internal static void Setup()
        {
            Weight = ViktorPlugin.Instance.Config.Bind(
                "Foxo",
                "Weight",
                100,
                "More it is higher, more there is a chance of him spawning. (Defaults to 100. For comparison, Baldi weight is 100) (Requires Restart)"
            );
        }
    }
}
