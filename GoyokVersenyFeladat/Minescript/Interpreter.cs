using GoyokVersenyFeladat.Interfaces;
using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Minescript
{
    internal class Interpreter
    {
        private readonly VariableManager variableManager;
        private readonly ExpressionEvaluator expressionEvaluator;
        private readonly IRobotController robotController;
        private int instructionCount= 0;
        private Stack<int> whileStack = new Stack<int>();
        public Interpreter(VariableManager variableManager, IRobotController robotController)
        {
            this.variableManager = variableManager;
            this.expressionEvaluator = new ExpressionEvaluator(variableManager,robotController);
            this.robotController = robotController;
        }

        public void Execute(List<ProgramLine> lines)
        {
            whileStack.Clear();
            instructionCount = 0;
            int currLineIndex = 0;
            while(currLineIndex < lines.Count)
            {
                instructionCount++;
                if(instructionCount > 1000)
                {
                    throw new InvalidOperationException("INSTRUCTION_COUNT_LIMIT_EXCEEDED");
                }
                ProgramLine currLine = lines[currLineIndex];
                if (currLine.Tokens.Count == 0)
                {
                    currLineIndex++;
                    continue;
                }
                string firstToken = currLine.Tokens[0].Value;
                if (firstToken == "set")
                {
                    ExecuteSet(currLine);
                    currLineIndex = AdvanceOrLoop(lines,currLineIndex);
                }
                else if(firstToken == "if")
                {
                    List<Token> ifTokens = currLine.Tokens.GetRange(1, currLine.Tokens.Count - 1);
                    object eredmeny = expressionEvaluator.EvaluateExpression(ifTokens);
                    if(eredmeny is bool isTrue)
                    {
                        if (isTrue)
                        {
                            currLineIndex++;
                        }
                        else
                        {
                            currLineIndex = SkipBlock(lines, currLineIndex);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("E_NON_BOOL_CONDITION");
                    }
                }
                else if(firstToken == "else")
                {
                    currLineIndex = SkipBlock(lines, currLineIndex);
                }
                else if (firstToken == "while")
                {
                    List<Token> ifTokens = currLine.Tokens.GetRange(1, currLine.Tokens.Count - 1);
                    object eredmeny = expressionEvaluator.EvaluateExpression(ifTokens);
                    if (eredmeny is bool isTrue)
                    {
                        if (isTrue)
                        {
                            if (whileStack.Count == 0 || whileStack.Peek() != currLineIndex)
                            {
                                whileStack.Push(currLineIndex);
                            }
                            currLineIndex++;
                        }
                        else
                        {
                            if (whileStack.Count > 0 && whileStack.Peek() == currLineIndex)
                            {
                                whileStack.Pop();
                            }
                            currLineIndex = SkipBlock(lines, currLineIndex);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("E_NON_BOOL_CONDITION");
                    }
                }
                else if (firstToken == "step_forward")
                {
                    robotController.StepForward();
                    currLineIndex = AdvanceOrLoop(lines, currLineIndex);
                }
                else if (firstToken == "turn_by")
                {
                    int degrees = int.Parse(currLine.Tokens[1].Value);
                    robotController.TurnBy(degrees);
                    currLineIndex = AdvanceOrLoop(lines, currLineIndex);
                }
                else if (firstToken == "mine_here")
                {
                    robotController.MineHere();
                    currLineIndex = AdvanceOrLoop(lines, currLineIndex);
                }
                else {
                    currLineIndex = AdvanceOrLoop(lines, currLineIndex);
                }
            }
        }

        /// <summary>
        /// Végrehajtja a 'set' utasítást, kiértékeli a megadott kifejezést, és eltárolja az értéket a változóban.
        /// </summary>
        /// <param name="line">A feldolgozandó programsor.</param>
        /// <exception cref="ArgumentException">Akkor váltódik ki, ha a sor szintaxisa érvénytelen (kevesebb mint 4 tokenből áll, vagy hiányzik a 'to' kulcsszó).</exception>
        public void ExecuteSet(ProgramLine line)
        {
            if (line.Tokens.Count < 4 || line.Tokens[2].Value != "to")
            {
                throw new ArgumentException("E_SYNTAX_ERROR");
            }
            string varname = line.Tokens[1].Value;
            List<Token> expressionTokens = line.Tokens.GetRange(3, line.Tokens.Count - 3);
            object value = expressionEvaluator.EvaluateExpression(expressionTokens);
            variableManager.SetVariable(varname, value);
        }

        /// <summary>
        /// Átugorja az aktuális behúzási szinthez tartozó blokkot, és visszaadja a blokk utáni első sor indexét.
        /// </summary>
        /// <param name="lines">A program sorainak listája.</param>
        /// <param name="startIndex">A blokkot kezdő sor indexe.</param>
        /// <returns>A blokk utáni következő sor sorszáma/indexe.</returns>
        private int SkipBlock(List<ProgramLine> lines, int startIndex)
        {
            int baseIndentLevel = lines[startIndex].IndentLevel;
            int currentIndex = startIndex + 1;
            while (currentIndex < lines.Count && lines[currentIndex].IndentLevel > baseIndentLevel)
            {
                currentIndex++;
            }
            return currentIndex;
        }

        /// <summary>
        /// Meghatározza a következőt végrehajtandó sor indexét, figyelembe véve az esetleges ciklusblokk (while) végét.
        /// </summary>
        /// <param name="lines">A program sorainak listája.</param>
        /// <param name="currentIndex">Az aktuális sor indexe.</param>
        /// <returns>A következő végrehajtandó sor sorszáma/indexe.</returns>
        private int AdvanceOrLoop(List<ProgramLine> lines, int currentIndex)
        {
            int nextIndex = currentIndex + 1;
            if (whileStack.Count > 0)
            {
                int loopFejIndex = whileStack.Peek();
                int loopIndent = lines[loopFejIndex].IndentLevel;
                if (nextIndex >= lines.Count || lines[nextIndex].IndentLevel <= loopIndent)
                {
                    return loopFejIndex;
                }
            }
            return nextIndex;
        }
    }
}
