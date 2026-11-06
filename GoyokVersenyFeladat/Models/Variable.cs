using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Models
{
    public enum VariableType
    {
        Int,
        Bool
    };
    internal class Variable
    {
        public Variable(VariableType type, object value)
        {
            Type = type;
            Value = value;
        }
        public VariableType Type { get; set; }
        public object Value { get; set; }
    }
}
