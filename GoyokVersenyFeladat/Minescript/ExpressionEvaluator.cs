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

        public ExpressionEvaluator(VariableManager variableManager, IRobotController robotController)
        {
            this.variableManager = variableManager;
            this.robotController = robotController;
        }

        /// <summary>
        /// Kigyűjti és kiértékeli egyetlen token értékét (szám vagy változó).
        /// </summary>
        /// <param name="token">A kiértékelendő token.</param>
        /// <returns>A token értéke objektumként (pl. egész szám).</returns>
        /// <exception cref="ArgumentException">Akkor dobódik, ha a token típusa érvénytelen.</exception>
        public object EvaluateToken(Token token)
        {
            if(token.Type == TokenType.Number)
            {
                return int.Parse(token.Value);
            }
            else if(token.Type == TokenType.Identifier)
            {
                return variableManager.GetVariable(token.Value).Value;
            }
            throw new ArgumentException("E_BAD_OPERAND");
        }

        /// <summary>
        /// Kiértékeli a tokenek listájából álló kifejezést.
        /// </summary>
        /// <param name="tokens">A kiértékelendő tokenek listája.</param>
        /// <returns>A kifejezés kiértékelésének az eredménye.</returns>
        /// <exception cref="ArgumentException">Akkor dobódik, ha a kifejezés üres, helytelen formátumú, érvénytelen operátort vagy értéket tartalmaz.</exception>
        /// <exception cref="DivideByZeroException">Akkor dobódik, ha nullával való osztás történik.</exception>
        public object EvaluateExpression(List<Token> tokens)
        {
            if(tokens.Count == 0)
            {
                throw new ArgumentException("E_BAD_EXPRESSION");
            }
            if (tokens.Count == 1)
            {
                return EvaluateToken(tokens[0]);
            }
            if (tokens[0].Value == "isThere")
            {
                return robotController.IsThere(tokens[1].Value);
            }

            object eredmeny = EvaluateToken(tokens[0]);
            for (int i = 1; i < tokens.Count; i += 2)
            {
                string op = tokens[i].Value; //operator beallitasa
                if (i + 1 >= tokens.Count)
                {
                    throw new ArgumentException("E_BAD_EXPRESSION");
                }
                object jobb = EvaluateToken(tokens[i + 1]);
                if (eredmeny is int balint && jobb is int jobbint)
                {
                    switch (op)
                    {
                        case "+":
                            eredmeny = balint + jobbint;
                            break;
                        case "-":
                            eredmeny = balint - jobbint;
                            break;
                        case "*":
                            eredmeny = balint * jobbint;
                            break;
                        case "/":
                            if (jobbint == 0) throw new DivideByZeroException();
                            eredmeny = balint / jobbint;
                            break;
                        case "<":
                            eredmeny = balint < jobbint;
                            break;
                        case ">":
                            eredmeny = balint > jobbint;
                            break;
                        case "==":
                            eredmeny = balint == jobbint;
                            break;
                        case "!=":
                            eredmeny = balint != jobbint;
                            break;
                        default:
                            throw new ArgumentException("E_BAD_OPERATOR");
                    }
                }
                else
                {
                    throw new ArgumentException("E_BAD_OPERAND");
                }
            }
            return eredmeny;
        }
    }
}
