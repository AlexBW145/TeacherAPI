using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeacherAPI;
using UnityEngine;

namespace TeacherExtension.Foxo.Items
{
    public class FireExtinguisher : Item
    {
        private Fog fog = new Fog()
        {
            color = new Color(0.752f, 0.809f, 0.8313726f),
            startDist = 5f,
            strength = 0f,
            maxDist = 100f,
            priority = 0,
        };
        private float previousMaxRaycast;
        private float time = 15f;
        private EnvironmentController ec;

        public override bool Use(PlayerManager pm)
        {
            if (FindObjectsOfType<FireExtinguisher>(false).Count(x => x != this) > 0 || !TeacherManager.Instance.SpoopModeActivated) // Jeez, why spam it??
            {
                Destroy(gameObject);
                return false;
            }
            ec = pm.ec;
            previousMaxRaycast = ec.MaxRaycast;
            ec.MaxRaycast = 25f;
            StartCoroutine(FadeOnFog());
            CoreGameManager.Instance.audMan.PlaySingle(Foxo.foxoAssets.Get<SoundObject>("fireextinguisher"));
            foreach (Foxo fox in FindObjectsOfType<Foxo>(false))
                fox.Sprayed();
            return true;
        }

        void Update()
        {
            time -= Time.deltaTime * ec.EnvironmentTimeScale;

            if (time <= 0f && time != -2f)
            {
                time = -2f;
                StartCoroutine(FadeOffFog());
            }
            
        }

        private IEnumerator FadeOnFog()
        {
            ec.AddFog(fog);
            float fogStrength2 = 0f;
            while (fogStrength2 < 1f)
            {
                fogStrength2 += 0.25f * Time.deltaTime;
                fog.strength = fogStrength2;
                ec.UpdateFog();
                yield return null;
            }

            fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
        }

        private IEnumerator FadeOffFog()
        {
            float fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
            while (fogStrength2 > 0f)
            {
                fogStrength2 -= 0.25f * Time.deltaTime;
                fog.strength = fogStrength2;
                ec.UpdateFog();
                yield return null;
            }

            fogStrength2 = 0f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
            if (ec.MaxRaycast == 25f) ec.MaxRaycast = previousMaxRaycast;
            ec.RemoveFog(fog);
            Destroy(gameObject);
        }
    }
}
