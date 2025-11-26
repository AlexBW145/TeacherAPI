using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace NullTeacher.Patches
{
    [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.EndGame))]
    internal class EndSequencePatch
    {
        internal static readonly FieldInfo
            _lives = AccessTools.DeclaredField(typeof(CoreGameManager), "lives"),
            _extraLives = AccessTools.DeclaredField(typeof(CoreGameManager), "extraLives");
        static bool Prefix(Transform player, Baldi baldi, CoreGameManager __instance)
        {
            if (baldi is NullTeacher)
            {
                Time.timeScale = 0f;
                __instance.musicMan.FlushQueue(true);
                MusicManager.Instance.StopMidi();
                __instance.disablePause = true;
                __instance.GetCamera(0).UpdateTargets(baldi.transform, 0);
                __instance.GetCamera(0).offestPos = (player.position - baldi.transform.position).normalized * 2f + Vector3.up;
                __instance.GetCamera(0).SetControllable(value: false);
                __instance.GetCamera(0).matchTargetRotation = false;
                __instance.audMan.volumeModifier = 0.6f;
                AudioManager audioManager = __instance.audMan;
                WeightedSelection<SoundObject>[] loseSounds = baldi.loseSounds;
                audioManager.PlaySingle(WeightedSelection<SoundObject>.RandomSelection(loseSounds));
                __instance.StartCoroutine(DeathCutscene(__instance));
                InputManager.Instance.Rumble(1f, 2f);
                HighlightManager.Instance.Highlight("steam_x", LocalizationManager.Instance.GetLocalizedText("Steam_Highlight_Lose"), string.Format(LocalizationManager.Instance.GetLocalizedText("Steam_Highlight_Lose_Desc"), LocalizationManager.Instance.GetLocalizedText(BaseGameManager.Instance.managerNameKey), LocalizationManager.Instance.GetLocalizedText(CoreGameManager.Instance.sceneObject.nameKey)), 2u, 0f, 0f, TimelineEventClipPriority.Standard);
                return false;
            }
            return true;
        }

        private static IEnumerator DeathCutscene(CoreGameManager __instance)
        {
            // Copy pasted from CoreGameManager lmao --Her, not me.
            int lif = (int)_lives.GetValue(__instance);
            int exlif = (int)_extraLives.GetValue(__instance);
            bool nolives = lif < 1 && exlif < 1;
            float time = 0f;
            float glitchRate = 0.5f;
            Shader.SetGlobalInt("_ColorGlitching", 1);
            Shader.SetGlobalInt("_SpriteColorGlitching", 1);
            if (PlayerFileManager.Instance.reduceFlashing)
            {
                Shader.SetGlobalInt("_ColorGlitchVal", Random.Range(0, 4096));
                Shader.SetGlobalInt("_SpriteColorGlitchVal", Random.Range(0, 4096));
            }
            yield return null;
            while (time <= 5f)
            {
                time += Time.unscaledDeltaTime * 0.5f;
                Shader.SetGlobalFloat("_VertexGlitchSeed", Random.Range(0f, 1000f));
                Shader.SetGlobalFloat("_TileVertexGlitchSeed", Random.Range(0f, 1000f));
                InputManager.Instance.Rumble(time / 5f, 0.05f);
                if (!PlayerFileManager.Instance.reduceFlashing)
                {
                    glitchRate -= Time.unscaledDeltaTime;
                    Shader.SetGlobalFloat("_VertexGlitchIntensity", Mathf.Pow(time, 2.2f));
                    Shader.SetGlobalFloat("_TileVertexGlitchIntensity", Mathf.Pow(time, 2.2f));
                    Shader.SetGlobalFloat("_ColorGlitchPercent", time * 0.05f);
                    Shader.SetGlobalFloat("_SpriteColorGlitchPercent", time * 0.05f);
                    if (glitchRate <= 0f)
                    {
                        Shader.SetGlobalInt("_ColorGlitchVal", Random.Range(0, 4096));
                        Shader.SetGlobalInt("_SpriteColorGlitchVal", Random.Range(0, 4096));
                        InputManager.Instance.SetColor(new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
                        glitchRate = 0.55f - time * 0.1f;
                    }
                }
                else
                {
                    Shader.SetGlobalFloat("_ColorGlitchPercent", time * 0.25f);
                    Shader.SetGlobalFloat("_SpriteColorGlitchPercent", time * 0.25f);
                    Shader.SetGlobalFloat("_VertexGlitchIntensity", time * 2f);
                    Shader.SetGlobalFloat("_TileVertexGlitchIntensity", time * 2f);
                }
                yield return null;
            }
            yield return null;
            __instance.GetCamera(0).camCom.farClipPlane = 1000f;
            __instance.GetCamera(0).billboardCam.farClipPlane = 1000f;
            __instance.GetCamera(0).StopRendering(true);
            if (nolives)
                Application.Quit();
            else
            {
                if (lif > 0)
                    _lives.SetValue(__instance, lif--);
                else
                    _extraLives.SetValue(__instance, exlif--);
                BaseGameManager.Instance.RestartLevel();
            }
            yield break;
        }
    }
}
