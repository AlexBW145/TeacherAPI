using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TeacherExtension.Viktor
{
    internal class ViktorTilePollutionManager : MonoBehaviour
    {
        private void Awake()
        {
            ec = GetComponent<EnvironmentController>();
        }

        private void Update()
        {
            Dictionary<Cell, float> newValues = new Dictionary<Cell, float>();
            foreach (Cell cell in pollutedCells.Keys.Where(j => pollutedCells.ContainsKey(j) && pollutedCells[j] > 0f))
                newValues[cell] = pollutedCells[cell] - Time.deltaTime * ec.EnvironmentTimeScale;
            pollutedCells = newValues;
        }

        public bool IsCellPolluted(Cell cell) => pollutedCells.ContainsKey(cell) && pollutedCells[cell] > 0f;

        public void PolluteCell(Cell cell, float duration) => pollutedCells[cell] = duration;

        private Dictionary<Cell, float> pollutedCells = new Dictionary<Cell, float>();

        private EnvironmentController ec;
    }
}
