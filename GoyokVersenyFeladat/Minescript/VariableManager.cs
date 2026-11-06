using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Minescript
{
    internal class VariableManager
    {
        private Dictionary<string, Variable> variables = new Dictionary<string, Variable>();

        public void SetVariable(string name, object value)
        {
            if (!variables.ContainsKey(name))
            {
                if (value is int)
                {
                    if ((int)value >= 0 && (int)value <= 100)
                    {
                        variables.Add(name, new Variable(VariableType.Int, value));
                    }
                    else
                    {
                        throw new ArgumentException("Integer értékének 0 és 100 között kell lennie");
                    }
                }
                else if (value is bool)
                {
                    variables.Add(name, new Variable(VariableType.Bool, value));
                }
                else
                {
                    throw new ArgumentException("Csak int és bool típusú változók engedélyezettek");
                }
            }
            else
            {
                Variable letezov = variables[name];
                if (letezov.Type == VariableType.Int)
                {
                    if (value is int)
                    {
                        if ((int)value >= 0 && (int)value <= 100)
                        {
                            letezov.Value = value;
                        }
                        else
                        {
                            throw new ArgumentException("Integer értékének 0 és 100 között kell lennie");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Csak int típusú változók engedélyezettek");
                    }
                }
                else if (letezov.Type == VariableType.Bool)
                {
                    if (value is bool)
                    {
                        letezov.Value = value;
                    }
                    else
                    {
                        throw new ArgumentException("Csak bool típusú változók engedélyezettek");
                    }
                }
            }
        }

        public Variable GetVariable(string name)
        {
            if (variables.ContainsKey(name))
            {
                return variables[name];
            }
            else
            {
                throw new KeyNotFoundException($"A(z) '{name}' nevű változó nem található");
            }
        }

        public bool HasVariable(string name)
        {
            return variables.ContainsKey(name);
        }
    }
}
