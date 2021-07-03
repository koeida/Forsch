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
            ["+"] = new Word(FAdd, false),
            ["*"] = new Word(FMult, false),
            ["."] = new Word(FDot, false),
            ["="] = new Word(FEq, false),
            [":"] = new Word(FWord, false),
            [";"] = new Word(FEndWord, true),
            ["["] = new Word(FForceCompile, false),
            ["]"] = new Word(FForceExecute, false),
            ["DUP"] = new Word(FDup, false),
            ["DROP"] = new Word(FDrop, false),
            ["ASSERT"] = new Word(FAssert, false),
            ["SWAP"] = new Word(FSwap, false),
            ["BRANCH"] = new Word(FBranch, false),
            ["BRANCH?"] = new Word(FBranchOnFalse, false),
            ["SURVEY"] = new Word(FSurvey, false),
            ["EMPTY?"] = new Word(FIsEmpty, false),
            ["HERE"] = new Word(FHere, false),
            ["!"] = new Word(FStore, false),
            [","] = new Word(FDictInsert, false),
            ["\""] = new Word(FToString, false),
            ["("] = new Word(FComment, false),
        };
        /// <summary>
        /// Duplicates the top element of the stack 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FDup(FEnvironment e)
        {
            var s = e.DataStack;
            s.Push(e.DataStack.Peek());
            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Swaps the top two elements of the stack. 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FSwap(FEnvironment e)
        {
            var s = e.DataStack;
            var top = s.Pop();
            var second = s.Pop();
            s.Push(top);
            s.Push(second);
            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Pops the top element off the data stack and immediately disposes of it
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FDrop(FEnvironment e)
        {
            var s = e.DataStack;
            s.Pop();
            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Like FDrop, but prints the top of the stack to the console before disposing of it.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FDot(FEnvironment e)
        {
            var s = e.DataStack;
            var (t, v) = s.Pop();
            System.Console.WriteLine(v);
            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Pops the top two elements of the stack.
        /// If they're not of the same type, throw an exception.
        /// If they're of the same type, pushes True if they're equal, False if they're not.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FEq(FEnvironment e)
        {
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
            if (xt != yt)
                throw new Exception($"Unable to compare equality of ({xt},{xv}) and ({yt}, {yv})");

            var res = xt switch
            {
                FType.FInt => System.Convert.ToInt32(xv) == System.Convert.ToInt32(yv),
                FType.FFloat => Math.Abs(System.Convert.ToSingle(xv) - System.Convert.ToSingle(yv)) < 0.0001,
                _ => xv == yv
            };
            
            s.Push((FType.FBool, res.ToString()));

            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Pops top two words off the stack and adds them together in a type-appropriate way.
        /// Pushes result on to stack.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        /// <exception cref="Exception">Throws exception if types don't match</exception>
        public static FEnvironment FAdd(FEnvironment e)
        {
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
            if (xt != yt)
                throw new Exception($"Unable to add ({xt},{xv}) and ({yt}, {yv}): Mismatched types");

            var res = xt switch
            {
                FType.FInt => (System.Convert.ToInt32(xv) + System.Convert.ToInt32(yv)).ToString(),
                FType.FFloat => $"{System.Convert.ToSingle(xv) + System.Convert.ToSingle(yv):0.0000}",
                FType.FStr => xv + yv,
                _ => throw new Exception($"Unable to add value of type {xt}")
            };
            s.Push((xt, res));
            
            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        public static FEnvironment FMult(FEnvironment e)
        {   
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
            if (xt != yt)
                throw new Exception($"Unable to multiple ({xt},{xv}) and ({yt}, {yv}): Type mismatch");

            var res = xt switch
            {
                FType.FInt => (System.Convert.ToInt32(xv) * System.Convert.ToInt32(yv)).ToString(),
                FType.FFloat => $"{System.Convert.ToSingle(xv) * System.Convert.ToSingle(yv):0.0000}",
                _ => throw new Exception(($"Can't multiply value of type {xt}"))
            };

            s.Push((xt, res));
            
            return new FEnvironment(s, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
            
        }

        public static int ReadIntParam(List<string> input, int curIndex)
        {
            if (!int.TryParse(input[curIndex], out var offset))
                throw new Exception("Attempted to branch with no branch offset");
            return offset;
        }

        /// <summary>
        /// A quirky word.
        /// The stack has to look like this: boolean BRANCHF int
        /// Pops the boolean (b) off the stack and reads the following int (i)
        /// If b is false, shift e.CurIndex to i
        /// Otherwise, return an environment with the input starting after the int
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        /// <exception cref="Exception">Throws exception if no boolean on the stack or if no integer following</exception>
        public static FEnvironment FBranchOnFalse(FEnvironment e)
        {
            var newIndex = ReadIntParam(e.Input, e.InputIndex);
            var (bt, bv) = e.DataStack.Pop();
            if (bt != FType.FBool)
                throw new Exception("Attempted to branch with non-boolean value");

            if (bv == "False")
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, newIndex + 1, e.CurWord, e.CurWordDef);
            else
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex + 1, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Shifts the input index to the  indicated by the next word in the input 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New Environment</returns>
        public static FEnvironment FBranch(FEnvironment e)
        {  
            var offset = ReadIntParam(e.Input, e.InputIndex);
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, offset, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Reads ahead in e.Input until it finds a closing parens,
        /// ignoring everything it finds.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FComment(FEnvironment e)
        {
            var newIndex = e.Input.IndexOf(")");

            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, newIndex + 1, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// "Surveys" the contents of the stack.
        /// That is, it outputs the whole stack:
        /// left is the bottommost, right is the topmost
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
                
            Console.Out.WriteLine(s);
            return e;
        }

        /// <summary>
        /// Pops the top of the stack.
        /// If it's False, throw an exception.
        /// If it's True, 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        /// <exception cref="Exception">Throws exception if top of stack is False (or not a boolean)</exception>
        public static FEnvironment FAssert(FEnvironment e)
        {
            var (bt, bv) = e.DataStack.Pop();
            if (bt == FType.FBool && bv == "True")
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
            else
                throw new Exception("Assert Failed");
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
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Pushes the current e.InputIndex to the stack
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FHere(FEnvironment e)
        {
            e.DataStack.Push((FType.FInt, (e.CurWordDef.Count() - 1).ToString()));
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Pops a string and an index off the stack
        /// and then inserts that string into e.CurWordDef at that index.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FStore(FEnvironment e)
        {
            var (_,s) = e.DataStack.Pop();
            var (_,i) = e.DataStack.Pop();
            e.CurWordDef[Convert.ToInt16(i)] = s;
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }

        /// <summary>
        /// Grabs the input up to the closing brace and appends it to CurWordDef
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
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
            
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, closingBraceIndex + 1, e.CurWord, newWordDef);
        }

        /// <summary>
        /// In compile mode, appends itself to CurWordDef
        /// In execute mode, Switches to compile mode
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FForceExecute(FEnvironment e)
        {
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, FMode.Execute, e.InputIndex, e.CurWord, e.CurWordDef);
        }
        

        /// <summary>
        /// Pops the top of the stack and appends it
        /// to the current word
        ///
        /// If the token is multi-word (judging by whitespace)
        /// the string is inserted as separate words
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FDictInsert(FEnvironment e)
        {
            var (_, s) = e.DataStack.Pop();
            if (s.Contains(" "))
            {
                var words = s.Split();
                var newWordDef = e.CurWordDef.Concat(words).ToList();
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, newWordDef);
            }
            else
            {
                var newWordDef = new List<string>(e.CurWordDef) {s};
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, newWordDef);
            }
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
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex, e.CurWord, e.CurWordDef);
        }
        
        /// <summary>
        /// Changes the mode to halt and adds the completed word to the dictionary.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>The same environment</returns>
        public static FEnvironment FEndWord(FEnvironment e)
        {
            e.WordDict[e.CurWord] = new Word(WordWrapper(e.CurWordDef), false);
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, FMode.Halt, e.InputIndex, null, null);
        }
        
        /// <summary>
        /// Creates a new word.
        /// 
        /// Reads in the rest of e.Input, wraps it with WordWrapper, and places it in
        /// e.WordDict. 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FWord(FEnvironment e)
        {
            var wordName = e.Input[e.InputIndex];
            int wordDataSkip; bool immediateMode;
            if (e.Input[e.InputIndex + 1] == "IMMEDIATE")
            {
                wordDataSkip = 2;
                immediateMode = true;
            }
            else
            {
                wordDataSkip = 1;
                immediateMode = false;
            }
            
            var wordData = e.Input.Skip(wordDataSkip).ToList();
            
            //Spin off a new interpreter in compile mode pointed at this fresh word data
            var newEnv = RunInterpreter(
                new FEnvironment(e.DataStack, e.WordDict, wordData, FMode.Compile, e.InputIndex, wordName, new List<string>()),
                () => null);

            newEnv.WordDict[wordName].IsImmediate = immediateMode;
            //Return back to normal execution context with our new word added to the word dictionary
            return new FEnvironment(newEnv.DataStack, newEnv.WordDict, new List<string>(), e.Mode, 0, null, null);
        }
        
    }
}