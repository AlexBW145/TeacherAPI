using HarmonyLib;
using System.Reflection;
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
            transform.position = Vector3.zero;
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
            if (cell == null || transform.position == Vector3.zero) return;
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

        private static readonly FieldInfo
            _hardCoverage = AccessTools.DeclaredField(typeof(Cell), "hardCoverage"),
            _softCoverage = AccessTools.DeclaredField(typeof(Cell), "softCoverage");
        private void OnDestroy()
        {
            if (transform.position != Vector3.zero && cell != null && !cell.HardCoverageFits(CellCoverage.Down))
            {
                CellCoverage hard = (CellCoverage)_hardCoverage.GetValue(cell);
                CellCoverage soft = (CellCoverage)_softCoverage.GetValue(cell);
                hard &= ~CellCoverage.Down;
                soft &= ~CellCoverage.Down;
                _hardCoverage.SetValue(cell, hard);
                _softCoverage.SetValue(cell, soft);
            }
            unaccessibleMang -= TempClose;
            accessibleMang -= TempOpen;
        }
    }
}