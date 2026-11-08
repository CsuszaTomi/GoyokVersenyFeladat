using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Interfaces
{
    internal interface ILogger
    {
        void Begin();                    //új futás: a napló törlése
        void BeginLine(int lineNumber);  //új naplósor a programsor számával
        void Append(string text);        //API név / paraméter / visszatérési érték (szóközzel elválasztva)
        void Error(string code);         //hibakód a sor végére
        void End(bool limitExceeded);    //EOR vagy INSTRUCTION_COUNT_LIMIT_EXCEEDED
        void Flush();                    //fájlba írás (nyomkövetésnél minden lépés után)
    }
}
