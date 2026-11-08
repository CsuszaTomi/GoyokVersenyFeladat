using GoyokVersenyFeladat.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoyokVersenyFeladat.Models
{
    internal class TesztRobot: IRobotController
    {
        public void StepForward()
        {
            MessageBox.Show("[ROBOT] Step forward");
        }

        public void TurnBy(int degrees)
        {
            MessageBox.Show($"[ROBOT] Turn by {degrees} degrees");
        }

        public void MineHere()
        {
            MessageBox.Show("[ROBOT] Mine here");
        }

        public bool IsThere(string objectType)
        {
            MessageBox.Show($"[ROBOT] Checking if '{objectType}' is there... (returning true)");
            return true;
        }
    }
}