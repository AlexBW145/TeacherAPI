using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TeacherExtension.Foxo.Items
{
    public class WaterBucketOfWater : Item
    {
        [SerializeField] internal SoundObject spill, wrongPlacement;
        private Cell cell;
        private EnvironmentController ec;
        private float timer = 10f;
        internal static EnvironmentController.TempObstacleManagement unaccessibleMang, accessibleMang;
        public override bool Use(PlayerManager pm)
        {
            ec = pm.ec;
            cell = pm.ec.CellFromPosition(pm.transform.position);
            if (cell.HardCoverageFits(CellCoverage.Down))
            {
                transform.position = cell.CenterWorldPosition;
                cell.HardCover(CellCoverage.Down);
                unaccessibleMang += TempClose;
                accessibleMang += TempOpen;
                CoreGameManager.Instance.audMan.PlaySingle(spill);
                return true;
            }
            CoreGameManager.Instance.audMan.PlaySingle(wrongPlacement);
            Destroy(gameObject);
            return false;
        }

        private void Update()
        {
            if (cell == null) return;
            timer -= Time.deltaTime * ec.EnvironmentTimeScale;
            if (timer <= 0f)
                Destroy(gameObject);
        }

        private void TempClose()
        {
            ec.FreezeNavigationUpdates(true);
            BlockSurroundingCells(true);
            ec.FreezeNavigationUpdates(false);
        }

        private void TempOpen()
        {
            ec.FreezeNavigationUpdates(true);
            BlockSurroundingCells(false);
            ec.FreezeNavigationUpdates(false);
        }

        private void BlockSurroundingCells(bool block)
        {
            for (int i = 0; i < 4; i++)
            {
                if (cell.ConstNavigable((Direction)i))
                    ec.CellFromPosition(cell.position + ((Direction)i).ToIntVector2()).Block(((Direction)i).GetOpposite(), block);
            }
        }

        private void OnDestroy()
        {
            unaccessibleMang -= TempClose;
            accessibleMang -= TempOpen;
        }
    }
}