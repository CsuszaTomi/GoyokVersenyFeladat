using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Interfaces
{
    internal interface IRobotController
    {
        void StepForward();
        void TurnBy(int fok);
        void MineHere();
        bool IsThere(string objectType);
    }
}
