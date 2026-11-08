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
        private const int MaxInstructions = 1000;
        private readonly VariableManager variableManager;
        private readonly ExpressionEvaluator expressionEvaluator;
        private readonly IRobotController robotController;
        private readonly ILogger logger;
        private readonly Action<string> printHandler;
        private readonly Stack<int> whileStack = new Stack<int>();
        private List<ProgramLine> lines = new List<ProgramLine>();
        private int instructionCount = 0;
        private int currLineIndex = 0;
        private bool elseTaken = false; //az if hamis volt, és az else sorra ugrottunk
        public bool IsFinished { get; private set; } = true; //a program végrehajtása befejeződött-e
        public string ErrorCode { get; private set; } //ha hiba történt, a hiba kódja
        public int CurrentLineNumber => IsFinished || currLineIndex >= lines.Count ? -1 : lines[currLineIndex].LineNumber;// A következőként végrehajtandó sor száma a forráskódban (kiemeléshez), -1 ha a futás véget ért.
        public int InstructionCount => instructionCount;//Eddig feldolgozott programsorok száma.

        public Interpreter(VariableManager variableManager, IRobotController robotController, ILogger logger, Action<string> printHandler = null)
        {
            this.variableManager = variableManager;
            this.robotController = robotController;
            this.logger = logger;
            this.printHandler = printHandler;
            this.expressionEvaluator = new ExpressionEvaluator(variableManager, robotController, logger);
        }

        /// <summary>
        /// Teljes futtatás az elejétől a végéig.
        /// </summary>
        public void Execute(List<ProgramLine> programLines)
        {
            Reset(programLines);
            while (Step())
            {
            }
            logger.Flush();
        }

        /// <summary>
        /// Változók aktuális állapota (név, megjelenített érték), létrehozásuk sorrendjében.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> Variables => variableManager.Snapshot();

        /// <summary>
        /// Egyetlen programsor végrehajtása (nyomkövetés és lassított futtatás), a napló minden lépés után fájlba kerül.
        /// Visszatérési érték: true, ha a futás még tart.
        /// </summary>
        public bool StepOnce()
        {
            bool running = Step();
            logger.Flush();
            return running;
        }

        /// <summary>
        /// Futó program leállítása kézi vezérlésnél: nem naplóz semmit, a napló az utolsó állapotában marad.
        /// </summary>
        public void Abort()
        {
            IsFinished = true;
        }

        /// <summary>
        /// Új futás előkészítése (nyomkövetéshez: ezután hívogatható a Step).
        /// </summary>
        public void Reset(List<ProgramLine> programLines)
        {
            lines = programLines;
            variableManager.Clear();
            whileStack.Clear();
            instructionCount = 0;
            currLineIndex = 0;
            elseTaken = false;
            ErrorCode = null;
            IsFinished = false;
            logger.Begin();
            FinishIfDone();
            logger.Flush();
        }

        private bool Step()
        {
            if (IsFinished)
            {
                return false;
            }
            ProgramLine currLine = lines[currLineIndex];
            instructionCount++;
            logger.BeginLine(currLine.LineNumber);
            try
            {
                ExecuteLine(currLine);
            }
            catch (MineScriptException ex)
            {
                logger.Error(ex.Code);
                logger.End(false);
                ErrorCode = ex.Code;
                IsFinished = true;
                return false;
            }
            FinishIfDone();
            return !IsFinished;
        }

        /// <summary>
        /// Lezárja a futást, ha nincs több sor (EOR), vagy ha az 1000 soros limit után még jönne egy sor.
        /// Közben némán átugorja azokat az else ágakat, amelyekbe az if igaz ága után "estünk bele".
        /// </summary>
        private void FinishIfDone()
        {
            while (currLineIndex < lines.Count && !elseTaken && lines[currLineIndex].Tokens[0].Value == "else")
            {
                currLineIndex = Next(SkipBlock(lines, currLineIndex));
            }
            if (currLineIndex >= lines.Count)
            {
                logger.End(false);
                IsFinished = true;
            }
            else if (instructionCount >= MaxInstructions)
            {
                logger.End(true);
                ErrorCode = "INSTRUCTION_COUNT_LIMIT_EXCEEDED";
                IsFinished = true;
            }
        }

        /// <summary>
        /// Az if/while feltétele: egyetlen simpleval, aminek logikainak kell lennie.
        /// </summary>
        private bool EvaluateCondition(ProgramLine line)
        {
            object value = expressionEvaluator.EvaluateToken(line.Tokens[1]);
            if (value is bool isTrue)
            {
                return isTrue;
            }
            throw new MineScriptException("E_NON_BOOL_CONDITION");
        }

        /// <summary>
        /// API-paraméter beolvasása (step forward / turn by): nem negatív egész kell.
        /// Naplózza a kiértékelt paramétert, rossz típus / negatív érték esetén E_BAD_ARG.
        /// </summary>
        private int ReadNonNegativeArg(Token param)
        {
            object value = expressionEvaluator.EvaluateToken(param); // E_UNINITIALIZED, ha nincs értéke
            logger.Append(ExpressionEvaluator.Format(value));
            if (value is int n && n >= 0)
            {
                return n;
            }
            throw new MineScriptException("E_BAD_ARG");
        }

        /// <summary>
        /// Végrehajtja a 'set' utasítást, kiértékeli a megadott kifejezést, és eltárolja az értéket a változóban.
        /// </summary>
        public void ExecuteSet(ProgramLine line)
        {
            if (line.Tokens.Count < 4 || line.Tokens[2].Value != "to")
            {
                throw new MineScriptException("E_SYNTAX_ERROR");
            }
            string varname = line.Tokens[1].Value;
            List<Token> expressionTokens = line.Tokens.GetRange(3, line.Tokens.Count - 3);
            object value = expressionEvaluator.EvaluateExpression(expressionTokens);
            variableManager.SetVariable(varname, value);
        }

        /// <summary>
        /// Átugorja az aktuális behúzási szinthez tartozó blokkot, és visszaadja a blokk utáni első sor indexét.
        /// </summary>
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
        /// A következő végrehajtandó sor: ha a megadott sor kívül esik a legbelső futó while törzsén,
        /// visszaugrik a while fejére. Minden "következő sor" meghatározásánál (SkipBlock után is) ezt kell használni.
        /// </summary>
        private int Next(int nextIndex)
        {
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

        /// <summary>
        /// Az aktuális programsor végrehajtása. A currLine paraméter a lines listából származik, és a currLineIndex mutatja, hogy melyik sor van éppen végrehajtás
        /// </summary>
        private void ExecuteLine(ProgramLine currLine)
        {
            List<Token> tokens = currLine.Tokens;
            string firstToken = tokens[0].Value;
            switch (firstToken)
            {
                case "set":
                    ExecuteSet(currLine);
                    currLineIndex = Next(currLineIndex + 1);
                    break;

                case "if":
                    if (EvaluateCondition(currLine))
                    {
                        currLineIndex = Next(currLineIndex + 1);
                    }
                    else
                    {
                        int after = SkipBlock(lines, currLineIndex);
                        if (after < lines.Count
                            && lines[after].Tokens[0].Value == "else"
                            && lines[after].IndentLevel == currLine.IndentLevel)
                        {
                            currLineIndex = after; //az else sora a következő lépésben kerül naplózásra
                            elseTaken = true;
                        }
                        else
                        {
                            currLineIndex = Next(after);
                        }
                    }
                    break;
                case "else":
                    //ide csak akkor jutunk, ha az if hamis volt (a többi esetet a FinishIfDone átugorja)
                    elseTaken = false;
                    currLineIndex = Next(currLineIndex + 1);
                    break;

                case "while":
                    if (EvaluateCondition(currLine))
                    {
                        if (whileStack.Count == 0 || whileStack.Peek() != currLineIndex)
                        {
                            whileStack.Push(currLineIndex);
                        }
                        currLineIndex = Next(currLineIndex + 1);
                    }
                    else
                    {
                        if (whileStack.Count > 0 && whileStack.Peek() == currLineIndex)
                        {
                            whileStack.Pop();
                        }
                        currLineIndex = Next(SkipBlock(lines, currLineIndex));
                    }
                    break;
                case "step": //step forward n
                    {
                        logger.Append("step forward");
                        int n = ReadNonNegativeArg(tokens[2]);
                        for (int i = 0; i < n; i++)
                        {
                            robotController.StepForward(); //falnál / pályaszélnél MineScriptException("E_BEDROCK_HIT")
                        }
                        currLineIndex = Next(currLineIndex + 1);
                        break;
                    }
                case "turn": //turn by n (n negyedfordulat balra)
                    {
                        logger.Append("turn by");
                        int n = ReadNonNegativeArg(tokens[2]);
                        robotController.TurnBy(n % 4);
                        currLineIndex = Next(currLineIndex + 1);
                        break;
                    }
                case "mine": //mine here
                    logger.Append("mine here");
                    robotController.MineHere(); //arany nélkül MineScriptException("E_NO_GOLD")
                    currLineIndex = Next(currLineIndex + 1);
                    break;
                case "print":
                    {
                        logger.Append("print");
                        string text = ExpressionEvaluator.Format(expressionEvaluator.EvaluateToken(tokens[1]));
                        logger.Append(text);
                        printHandler?.Invoke(text);
                        currLineIndex = Next(currLineIndex + 1);
                        break;
                    }
                default:
                    throw new MineScriptException("E_SYNTAX_ERROR");
            }
        }
    }
}