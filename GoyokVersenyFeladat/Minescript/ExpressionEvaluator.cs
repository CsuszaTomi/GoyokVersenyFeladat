using GoyokVersenyFeladat.Interfaces;
using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Minescript
{
    internal class ExpressionEvaluator
    {
        private readonly VariableManager variableManager;
        private readonly IRobotController robotController;
        private readonly ILogger logger;

        public ExpressionEvaluator(VariableManager variableManager, IRobotController robotController, ILogger logger)
        {
            this.variableManager = variableManager;
            this.robotController = robotController;
            this.logger = logger;
        }

        /// <summary>
        /// Érték naplózható / kiírható alakja (true/false kisbetűvel).
        /// </summary>
        public static string Format(object value)
        {
            if (value is bool b) return b ? "true" : "false";
            return value.ToString();
        }


        /// <summary>
        /// Egyetlen simpleval kiértékelése (szám, logikai literál vagy változó).
        /// </summary>
        /// <exception cref="MineScriptException">E_UNINITIALIZED, ha a változónak még nincs értéke.</exception>
        public object EvaluateToken(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    return int.Parse(token.Value);
                case TokenType.Boolean:
                    return token.Value == "true";
                case TokenType.Identifier:
                    return variableManager.GetVariable(token.Value).Value;
            }
            throw new MineScriptException("E_BAD_OPERAND");
        }


        /// <summary>
        /// A set utasítás 'to' utáni részének (calculatedval) kiértékelése balról jobbra:
        /// simpleval | isThere x | not x | a op b. Az operandusokat egyenként olvassa és ellenőrzi,
        /// az első hibánál megáll.
        /// </summary>
        public object EvaluateExpression(List<Token> tokens)
        {
            if (tokens.Count == 1)
            {
                return EvaluateToken(tokens[0]);
            }
            if (tokens.Count == 2 && tokens[0].Value == "isThere")
            {
                return CallIsThere(tokens[1]);
            }
            if (tokens.Count == 2 && tokens[0].Value == "not")
            {
                return !(bool)ReadOperand(tokens[1], typeof(bool));
            }
            if (tokens.Count == 3)
            {
                return EvaluateBinary(tokens[0], tokens[1].Value, tokens[2]);
            }
            throw new MineScriptException("E_BAD_OPERAND");
        }

        private object EvaluateBinary(Token leftToken, string op, Token rightToken)
        {
            switch (op)
            {
                case "+":
                case "-":
                case "*":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    {
                        int a = (int)ReadOperand(leftToken, typeof(int));
                        int b = (int)ReadOperand(rightToken, typeof(int));
                        switch (op)
                        {
                            case "+": return a + b;
                            case "-": return a - b;
                            case "*": return a * b;
                            case "<": return a < b;
                            case "<=": return a <= b;
                            case ">": return a > b;
                            default: return a >= b;
                        }
                    }
                case "and":
                case "or":
                    {
                        //a másodikat akkor is beolvassa, ha az eredmény az elsőből már következne
                        bool a = (bool)ReadOperand(leftToken, typeof(bool));
                        bool b = (bool)ReadOperand(rightToken, typeof(bool));
                        return op == "and" ? (a && b) : (a || b);
                    }
                case "=":
                case "<>":
                    {
                        object a = ReadOperand(leftToken, null);
                        object b = ReadOperand(rightToken, null);
                        if (a.GetType() != b.GetType())
                        {
                            throw new MineScriptException("E_BAD_OPERAND");
                        }
                        bool equal = a.Equals(b);
                        return op == "=" ? equal : !equal;
                    }
            }
            throw new MineScriptException("E_BAD_OPERAND");
        }

        /// <summary>
        /// Operandus beolvasása és azonnali típusellenőrzése (required == null: bármi jó).
        /// </summary>
        private object ReadOperand(Token token, Type required)
        {
            object value = EvaluateToken(token);
            if (required != null && value.GetType() != required)
            {
                throw new MineScriptException("E_BAD_OPERAND");
            }
            return value;
        }



        /// <summary>
        /// isThere w: naplózza az API-hívást (név, paraméter, visszatérési érték), és meghívja a robotot.
        /// Naplóformátum: "isThere 2 false", "isThere 3 E_BAD_ARG", "isThere E_UNINITIALIZED".
        /// </summary>
        private bool CallIsThere(Token param)
        {
            logger.Append("isThere");
            object w = EvaluateToken(param); // nem inicializált változó: E_UNINITIALIZED, paraméter nélkül
            logger.Append(Format(w));
            if (!(w is int wi) || wi < 0 || wi > 2)
            {
                throw new MineScriptException("E_BAD_ARG");
            }
            bool result = robotController.IsThere(wi.ToString());
            logger.Append(Format(result));
            return result;
        }
    }   
}
