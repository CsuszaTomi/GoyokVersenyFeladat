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
        private List<string> order = new List<string>(); //létrehozás sorrendje (a nyomkövető nézethez)

        /// <summary>
        /// Új futás előtt törli a változókat.
        /// </summary>
        public void Clear()
        {
            variables.Clear();
            order.Clear();
        }

        /// <summary>
        /// A változók pillanatnyi állapota (név, megjelenített érték) létrehozásuk sorrendjében.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> Snapshot()
        {
            return order
                .Select(n => new KeyValuePair<string, string>(n, ExpressionEvaluator.Format(variables[n].Value)))
                .ToList();
        }

        /// <summary>
        /// Értékadás. Első értékadásnál létrejön a változó, és a típusa véglegesen rögzül.
        /// </summary>
        /// <exception cref="MineScriptException">E_TYPE_MISMATCH, ha a típus eltér a rögzítettől.</exception>
        public void SetVariable(string name, object value)
        {
            VariableType type = value is bool ? VariableType.Bool : VariableType.Int;
            if (!variables.TryGetValue(name, out Variable existing))
            {
                variables.Add(name, new Variable(type, value));
                order.Add(name);
            }
            else if (existing.Type != type)
            {
                throw new MineScriptException("E_TYPE_MISMATCH");
            }
            else
            {
                existing.Value = value;
            }
        }


        /// <summary>
        /// Retrieves a variable by its name.
        /// </summary>
        /// <param name="name">The name of the variable to retrieve. This value cannot be <see langword="null"/> or empty.</param>
        /// <returns>The <see cref="Variable"/> associated with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a variable with the specified <paramref name="name"/> does not exist.</exception>
        public Variable GetVariable(string name)
        {
            if (variables.TryGetValue(name, out Variable v))
            {
                return v;
            }
            throw new MineScriptException("E_UNINITIALIZED");
        }


        /// <summary>
        /// Determines whether a variable with the specified name exists in the collection.
        /// </summary>
        /// <param name="name">The name of the variable to check for. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if a variable with the specified name exists; otherwise, <see langword="false"/>.</returns>
        public bool HasVariable(string name)
        {
            return variables.ContainsKey(name);
        }
    }
}