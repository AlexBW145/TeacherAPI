#if DEBUG
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeacherExtension.Foxo.Items
{
    public class WaterBucketOfWater : Item
    {
        [SerializeField] internal SoundObject spill;
        [SerializeField] internal SoundObject wrongPlacement;
        public override bool Use(PlayerManager pm)
        {
            if (pm.ec.CellFromPosition(pm.transform.position).SoftCoverageFits(CellCoverage.Down))
            {
                return true;
            }
            CoreGameManager.Instance.audMan.PlaySingle(wrongPlacement);
            Destroy(gameObject);
            return false;
        }
    }
}
#endif