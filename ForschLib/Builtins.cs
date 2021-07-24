using System;
using System.Collections.Generic;
using System.Linq;
using static Forsch.Interpreter;

namespace Forsch
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Word>;
    
    public static class Builtins
    {
        /// <summary>
        /// Built-in words.
        /// The boolean indicates whether it's called in immediate mode or not:
        /// that is, whether it'll be interpreted/executed even if we're in the middle
        /// of compiling a word.
        /// </summary>
        public static FWordDict BuiltinWords = new FWordDict
        {
            ["+"] = new Word(FAdd, false, null),
            ["*"] = new Word(FMult, false, null),
            ["/"] = new Word(FDiv, false, null),
            ["."] = new Word(FDot, false, null),
            ["="] = new Word(FEq, false, null),
            [":"] = new Word(FWord, false, null),
            [";"] = new Word(FEndWord, true, null),
            ["["] = new Word(FForceCompile, false, null),
            ["]"] = new Word(FForceExecute, false, null),
            ["RAND"] = new Word(FRandInt,false, null),
            ["DUP"] = new Word(FDup, false, null),
            ["DROP"] = new Word(FDrop, false, null),
            ["ASSERT"] = new Word(FAssert, false, null),
            ["SWAP"] = new Word(FSwap, false, null),
            ["OVER"] = new Word(FOver, false, null),
            ["ROT"] = new Word(FRot, false, null),
            ["PICK"] = new Word(FPick, false, null),
            ["BRANCH"] = new Word(FBranch, false, null),
            ["BRANCH?"] = new Word(FBranchOnFalse, false, null),
            ["SURVEY"] = new Word(FSurvey, false, null),
            ["EMPTY?"] = new Word(FIsEmpty, false, null),
            ["HERE"] = new Word(FHere, false, null),
            ["!"] = new Word(FStore, false, null),
            [","] = new Word(FDictInsert, false, null),
            ["\""] = new Word(FToString, false, null),
            ["("] = new Word(FComment, false, null),
        };

        /// <summary>
        /// Duplicates the top element of the stack 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FDup(FEnvironment e)
        {
            e.DataStack.Push(e.DataStack.Peek());
            return e;
        }

        /// <summary>
        /// Swaps the top two elements of the stack. 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FSwap(FEnvironment e)
        {
            var s = e.DataStack;
            var top = s.Pop();
            var second = s.Pop();
            s.Push(top);
            s.Push(second);
            return e;
        }

        /// <summary>
        /// Copies second item of stack to top of stack.
        /// ( n1 n2 -- n1 n2 n1 )
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FOver(FEnvironment e)
        {
            var n2 = e.DataStack.Pop();
            var n1 = e.DataStack.Pop();
            e.DataStack.Push(n1);
            e.DataStack.Push(n2);
            e.DataStack.Push(n1);
            return e;
        }

        /// <summary>
        /// Brings third element of stack to front
        /// (n1 n2 n3 -- n2 n3 n1)
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FRot(FEnvironment e)
        {
            var n3 = e.DataStack.Pop();
            var n2 = e.DataStack.Pop();
            var n1 = e.DataStack.Pop();
            e.DataStack.Push(n2);
            e.DataStack.Push(n3);
            e.DataStack.Push(n1);

            return e;
        }

        /// <summary>
        /// Pops a number n off the stack
        /// and copies the nth element of the stack to the top
        /// of the stack.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FPick(FEnvironment e)
        {
            var (_, nstr) = e.DataStack.Pop();
            var n = Convert.ToInt16(nstr);
            var nth = e.DataStack.Skip(n).First();
            e.DataStack.Push(nth);
            return e;
        }

        /// <summary>
        /// Pops the top element off the data stack and immediately disposes of it
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FDrop(FEnvironment e)
        {
            e.DataStack.Pop();
            return e;
        }

        /// <summary>
        /// Like FDrop, but prints the top of the stack to the console before disposing of it.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FDot(FEnvironment e)
        {
            var (t, v) = e.DataStack.Pop();
            e.Output = v;
            return e;
        }

        /// <summary>
        /// Pops the top two elements of the stack.
        /// If they're not of the same type, throw an exception.
        /// If they're of the same type, pushes True if they're equal, False if they're not.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FEq(FEnvironment e)
        {
            var (xt, xv) = e.DataStack.Pop();
            var (yt, yv) = e.DataStack.Pop();
            if (xt != yt)
                throw new Exception($"Unable to compare equality of ({xt},{xv}) and ({yt}, {yv})");

            var res = xt switch
            {
                FType.FInt => Convert.ToInt32(xv) == Convert.ToInt32(yv),
                FType.FFloat => Math.Abs(Convert.ToSingle(xv) - Convert.ToSingle(yv)) < 0.0001,
                _ => xv == yv
            };
            
            e.DataStack.Push((FType.FBool, res.ToString()));

            return e;
        }

        /// <summary>
        /// Pops the top two numbers (min max) off the stack and
        /// pushes a random integer within min max back onto the stack
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static FEnvironment FRandInt(FEnvironment e)
        {
            var (xt, xv) = e.DataStack.Pop();
            var (yt, yv) = e.DataStack.Pop();
            if (!(xt == yt && xt == FType.FInt))
                throw new Exception($"Type error: Unable to generate a random integer with ({xt},{xv}) and ({yt}, {yv})");
            
            var rval = new Random().Next(Convert.ToInt16(yv), Convert.ToInt16(xv));
            e.DataStack.Push((FType.FInt, rval.ToString()));

            return e;
        }

        /// <summary>
        /// Pops top two words off the stack and adds them together in a type-appropriate way.
        /// Pushes result on to stack.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        /// <exception cref="Exception">Throws exception if types don't match</exception>
        public static FEnvironment FAdd(FEnvironment e)
        {
            var (xt, xv) = e.DataStack.Pop();
            var (yt, yv) = e.DataStack.Pop();
            if (xt != yt)
                throw new Exception($"Unable to add ({xt},{xv}) and ({yt}, {yv}): Mismatched types");

            var res = xt switch
            {
                FType.FInt => (Convert.ToInt32(yv) + Convert.ToInt32(xv)).ToString(),
                FType.FFloat => $"{Convert.ToSingle(yv) + Convert.ToSingle(xv):0.0000}",
                FType.FStr => yv + xv,
                _ => throw new Exception($"Unable to add value of type {xt}")
            };
            
            e.DataStack.Push((xt, res));

            return e;
        }

        public static FEnvironment FMult(FEnvironment e)
        {   
            var (xt, xv) = e.DataStack.Pop();
            var (yt, yv) = e.DataStack.Pop();
            if (xt != yt)
                throw new Exception($"Unable to multiply ({xt},{xv}) and ({yt}, {yv}): Type mismatch");

            var res = xt switch
            {
                FType.FInt => (Convert.ToInt32(xv) * Convert.ToInt32(yv)).ToString(),
                FType.FFloat => $"{Convert.ToSingle(xv) * Convert.ToSingle(yv):0.0000}",
                _ => throw new Exception(($"Can't multiply value of type {xt}"))
            };

            e.DataStack.Push((xt, res));

            return e;
        }
        
        /// <summary>
        /// Divides top two words of stack and pushes result
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static FEnvironment FDiv(FEnvironment e)
        {   
            var (xt, xv) = e.DataStack.Pop();
            var (yt, yv) = e.DataStack.Pop();
            if (xt != yt)
                throw new Exception($"Unable to divide ({xt},{xv}) and ({yt}, {yv}): Type mismatch");

            var res = xt switch
            {
                FType.FInt => (Convert.ToInt32(yv) / Convert.ToInt32(xv)).ToString(),
                FType.FFloat => $"{Convert.ToSingle(yv) / Convert.ToSingle(xv):0.0000}",
                _ => throw new Exception(($"Can't multiply value of type {xt}"))
            };

            e.DataStack.Push((xt, res));

            return e;
        }

        public static int ReadIntParam(List<string> input, int curIndex)
        {
            if (!int.TryParse(input[curIndex], out var parameter))
                throw new Exception("Expected word " + input[curIndex] + " to be an integer.");
            return parameter;
        }

        /// <summary>
        /// A quirky word.
        /// The stack has to look like this: boolean BRANCHF int
        /// Pops the boolean (b) off the stack and reads the following int (i)
        /// If b is false, shift e.CurIndex to i
        /// Otherwise, return an environment with the input starting after the int
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        /// <exception cref="Exception">Throws exception if no boolean on the stack or if no integer following</exception>
        public static FEnvironment FBranchOnFalse(FEnvironment e)
        {
            var newIndex = ReadIntParam(e.Input, e.InputIndex);
            var (bt, bv) = e.DataStack.Pop();
            if (bt != FType.FBool)
                throw new Exception("Attempted to branch with non-boolean value");

            if (bv == "False")
                e.InputIndex = newIndex + 1;
            else
                e.InputIndex += 2;
            return e;
        }

        /// <summary>
        /// Shifts InputIndex to the next integer value in the current input.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New Environment</returns>
        public static FEnvironment FBranch(FEnvironment e)
        {  
            var offset = ReadIntParam(e.Input, e.InputIndex);
            e.InputIndex = offset;
            return e;
        }

        /// <summary>
        /// Reads ahead in e.Input until it finds a closing parens,
        /// ignoring everything it finds.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FComment(FEnvironment e)
        {
            e.InputIndex = e.Input.IndexOf(")") + 1;
            return e;
        }

        /// <summary>
        /// "Surveys" the contents of the stack.
        /// That is, it outputs the whole stack
        /// as a series of (type, value) pairs.
        /// left is the bottommost element of the stack, right is the topmost
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>The same, unchanged environment</returns>
        public static FEnvironment FSurvey(FEnvironment e)
        {
            var s = e
                .DataStack
                .Reverse()
                .Aggregate("", (a, x) => a + $" ({x.Item1},{x.Item2})")
                .Trim();

            e.Output = s;
            return e;
        }

        /// <summary>
        /// Pops the top of the stack.
        /// If it's False, throw an exception.
        /// If it's True, 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        /// <exception cref="Exception">Throws exception if top of stack is False (or not a boolean)</exception>
        public static FEnvironment FAssert(FEnvironment e)
        {
            var (bt, bv) = e.DataStack.Pop();
            if (bt == FType.FBool && bv == "True")
                return e;
            else
                throw new Exception("Assert Failed on line: \n" + String.Join(" ", e.Input));
        }

        /// <summary>
        /// Returns true if stack is empty, false otherwise.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static FEnvironment FIsEmpty(FEnvironment e)
        {
            var isEmpty = !e.DataStack.Any();
            e.DataStack.Push((FType.FBool, isEmpty.ToString()));
            return e;
        }

        /// <summary>
        /// Pushes the current e.InputIndex to the stack
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FHere(FEnvironment e)
        {
            e.DataStack.Push((FType.FInt, (e.CurWordDef.Count() - 1).ToString()));
            return e;
        }

        /// <summary>
        /// Pops a string and an index off the stack
        /// and then inserts that string into e.CurWordDef at that index.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FStore(FEnvironment e)
        {
            var (_,s) = e.DataStack.Pop();
            var (_,i) = e.DataStack.Pop();
            e.CurWordDef[Convert.ToInt16(i)] = s;
            return e;
        }

        /// <summary>
        /// Grabs the input up to the closing brace and appends it to CurWordDef
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FForceCompile(FEnvironment e)
        {
            var closingBraceIndex = -1;
            for(var x = e.InputIndex; x < e.Input.Count(); x++)
            {
                if (e.Input[x] == "]")
                {
                    closingBraceIndex = x;
                    break;
                }
            }

            if (closingBraceIndex == -1)
                throw new Exception("Parsing error: no closing brace ]");

            var newWordDef = e.CurWordDef.Concat(e.Input.GetRange(e.InputIndex, closingBraceIndex - 1)).ToList();

            e.InputIndex = closingBraceIndex + 1;
            e.CurWordDef = newWordDef;
            return e;
        }

        /// <summary>
        /// Switches environment to execute mode
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FForceExecute(FEnvironment e)
        {
            e.Mode = FMode.Execute;
            return e;
        }


        /// <summary>
        /// Pops the top of the stack and appends it
        /// to the current word
        ///
        /// If the token is multi-word (judging by whitespace)
        /// the string is inserted as separate words
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FDictInsert(FEnvironment e)
        {
            var (_, s) = e.DataStack.Pop();
            if (s.Contains(" "))
            {
                var words = s.Split();
                var newWordDef = e.CurWordDef.Concat(words).ToList();
                e.CurWordDef = newWordDef;
            }
            else
            {
                var newWordDef = new List<string>(e.CurWordDef) {s};
                e.CurWordDef = newWordDef;
            }

            return e;
        }

        /// <summary>
        /// Pops a word off the stack, replaces the underscores in it, and pushes it back
        /// in, replacing spaces with underscores.
        /// The lazy way to have strings with whitespace strings. Very nonstandard, but
        /// this is my little baby Forth.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static FEnvironment FToString(FEnvironment e)
        {
            var (_, s) = e.DataStack.Pop();
            var result = s.Replace("_", " ");
            e.DataStack.Push((FType.FStr, result));
            return e;
        }

        /// <summary>
        /// Changes the mode to halt and adds the completed word to the dictionary.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>The same environment</returns>
        public static FEnvironment FEndWord(FEnvironment e)
        {
            var stringDef = new List<string>(e.CurWordDef);
            e.WordDict[e.CurWord] = new Word(null, false, stringDef.ToArray());
            e.CurWord = "";
            e.CurWordDef = new List<string>();
            e.Mode = FMode.Halt;
            return e;
        }

        /// <summary>
        /// Creates a new word.
        /// 
        /// Reads in the rest of e.Input, wraps it with WordWrapper, and places it in
        /// e.WordDict. 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment FWord(FEnvironment e)
        {
            var wordName = e.Input[e.InputIndex];

            e.Mode = FMode.Compile;
            e.CurWord = wordName;
            e.InputIndex += 1;
            return e;

            // var wordData = e.Input.Skip(wordDataSkip).ToList();
            // 
            // //Spin off a new interpreter in compile mode pointed at this fresh word data
            // var newEnv = RunInterpreter(
            //     new FEnvironment(e.DataStack, e.WordDict, wordData, FMode.Compile, e.InputIndex, wordName, new List<string>(), e.WriteLine),
            //     () => null);

            // newEnv.WordDict[wordName].IsImmediate = immediateMode;
            // //Return back to normal execution context with our new word added to the word dictionary
            // return new FEnvironment(newEnv.DataStack, newEnv.WordDict, new List<string>(), e.Mode, 0, null, null, e.WriteLine);
        }
    }
}