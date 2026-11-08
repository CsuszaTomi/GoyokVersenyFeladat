using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Minescript
{
    /// <summary>
    /// MineScript futásidejű hiba. A Code a naplóba kerülő hibakód (pl. E_BAD_ARG).
    /// A robot (IRobotController) is ezt dobja: E_BEDROCK_HIT, E_NO_GOLD.
    /// </summary>
    internal class MineScriptException : Exception
    {
        public string Code { get; }

        public MineScriptException(string code) : base(code)
        {
            Code = code;
        }
    }

}
