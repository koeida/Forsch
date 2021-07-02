using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;

namespace Forcsh
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Func<FEnvironment, FEnvironment>>;

    public enum FMode
    {
        Halt,
        Eval
    };
    
    public enum FType
    {
        FStr,
        FFloat,
        FInt,
        FWord,
        FNull,
        FBool
    }
    
    public readonly struct FEnvironment
    {
        public FStack DataStack { get; }
        public FWordDict WordDict { get; }
        public IEnumerable<String> Input { get; }
        public FMode Mode { get; }
        
        public FEnvironment(FStack dataStack, FWordDict wordDict, IEnumerable<string> input, FMode mode)
        {
            //Note to self: Let there be a debug mode that changes this constructor to assign *copies* of all the arguments.
            DataStack = dataStack;
            WordDict = wordDict;
            Input = input;
            Mode = mode;
        }
    }

    public static class Program
    {
        public static FStack DataStack = new FStack();

        /// <summary>
        /// Duplicates the top element of the stack 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FDup(FEnvironment e)
        {
            var s = e.DataStack;
            s.Push(DataStack.Peek());
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
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

            var res = false;
            if (xt == FType.FInt)
                res = System.Convert.ToInt32(xv) == System.Convert.ToInt32(yv);
            else if (xt == FType.FFloat)
                res = Math.Abs(System.Convert.ToSingle(xv) - System.Convert.ToSingle(yv)) < 0.0001;
            else if (new FType[] {FType.FStr, FType.FWord, FType.FBool}.Contains(xt) )
                res = xv == yv;
            
            s.Push((FType.FBool, res.ToString()));

            return new FEnvironment(s, WordDict, e.Input, e.Mode);
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
                throw new Exception($"Unable to compare equality of ({xt},{xv}) and ({yt}, {yv})");

            var res = "";
            if (xt == FType.FInt)
                res = (System.Convert.ToInt32(xv) + System.Convert.ToInt32(yv)).ToString();
            else if (xt == FType.FFloat)
                res = $"{System.Convert.ToSingle(xv) + System.Convert.ToSingle(yv):0.0000}";
            else if (new FType[] {FType.FStr, FType.FWord, FType.FBool}.Contains(xt))
                res = (xv == yv).ToString();
            s.Push((xt, res));
            
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        /// <summary>
        /// A quirky word.
        /// The stack has to look like this: boolean BRANCHF int
        /// Pops the boolean (b) off the stack and reads the following int (i)
        /// If b is false, jump e.Input ahead i words past i.
        /// Otherwise, return an environment with the input starting after the int
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        /// <exception cref="Exception">Throws exception if no boolean on the stack or if no integer following</exception>
        public static FEnvironment FBranchOnFalse(FEnvironment e)
        {
            if (!int.TryParse(e.Input.First(), out var offset))
                throw new Exception("Attempted to branch with no branch offset");
            
            var tail = e.Input.Skip(1);
            var (bt, bv) = e.DataStack.Pop();
            if (bt != FType.FBool)
                throw new Exception("Attempted to branch with non-boolean value");
            
            if (bv == "False")
            {
                var newInput = tail.Skip(offset - 1);
                return new FEnvironment(e.DataStack, e.WordDict, newInput, e.Mode);
            }
            else
            {
                return new FEnvironment(e.DataStack, e.WordDict, tail, e.Mode);
            }
        }

        /// <summary>
        /// Reads ahead in e.Input until it finds a closing parens,
        /// ignoring everything it finds.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FComment(FEnvironment e)
        {
            var tail = e.Input
                .SkipWhile(w => w != ")")
                .Skip(1);
            return new FEnvironment(e.DataStack, e.WordDict, tail, e.Mode);
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
                .Aggregate("", (a, x) => a + " " + x.Item2)
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
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode);
            else
                throw new Exception("FAssert Failed");
        }

        /// <summary>
        /// Doesn't do anything. Only included because traditionally
        /// you put a ; at the end of a new word definition,
        /// but in this implementation I don't actually need ; to do anything.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>The same environment</returns>
        public static FEnvironment FSike(FEnvironment e)
        {
            return e;
        }

        /// <summary>
        /// Takes the definition of a word (name excluded) and wraps it in a function
        /// that:
        /// * Takes the current environment,
        /// * Assigns wordData (the word definition) to e.Input,
        /// * Runs a read/eval loop on that definition until it finishes,
        /// * Then returns control to the calling context/input.
        /// </summary>
        /// <param name="wordData">The word definition</param>
        /// <returns>Function that shifts environment to word definition</returns>
        public static Func<FEnvironment, FEnvironment> WordWrapper(IEnumerable<String> wordData)
        {
            return (FEnvironment e) =>
            {
                var oldInput = e.Input;
                var tempEnv = new FEnvironment(e.DataStack, e.WordDict, wordData, e.Mode);
                
                var resultEnv = RunInterpreter(tempEnv, () => null);

                return new FEnvironment(resultEnv.DataStack, resultEnv.WordDict, oldInput, resultEnv.Mode);
            };
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
            var wordName = e.Input.First();
            var wordData = e.Input.Skip(1);
            e.WordDict[wordName] = WordWrapper(wordData);

            return new FEnvironment(e.DataStack, e.WordDict, new string[] { }, e.Mode);
        }

        /// <summary>
        /// Built-in words.
        /// </summary>
        public static FWordDict WordDict = new FWordDict
        {
            ["+"] = FAdd,
            ["."] = FDot,
            ["="] = FEq,
            [":"] = FWord,
            [";"] = FSike,
            ["DUP"] = FDup,
            ["DROP"] = FDrop,
            ["ASSERT"] = FAssert,
            ["SWAP"] = FSwap,
            ["BRANCHF"] = FBranchOnFalse,
            ["SURVEY"] = FSurvey,
            ["("] = FComment,
        };
        
        /// <summary>
        /// Reads a string s and converts it into a (FType, value) token,
        /// which is the only type allowed on the stack.
        ///
        /// The values always stay as strings, but Tokenize is intended to reliably
        /// attach FType values to them indicating whether the string can be coerced into
        /// a value of the intended type.
        /// </summary>
        /// <param name="s">The string to tokenize</param>
        /// <param name="wordDict">The word dictionary</param>
        /// <returns>A new (Ftype, String) tuple</returns>
        public static (FType, String) Tokenize(string s, FWordDict wordDict)
        {
            if (wordDict.ContainsKey(s))
            {
                return (FType.FWord, s);
            }
            else
            {
                int i; float f; bool b;
                
                if (int.TryParse(s, out i))
                    return (FType.FInt, s);
                else if (float.TryParse(s, out f))
                    return (FType.FFloat, s);
                else if (bool.TryParse(s, out b))
                    return (FType.FBool, s);
                else
                    return (FType.FStr, s);
            }
        }
        
        /// <summary>
        /// Tokenizes the 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static FEnvironment Eval(FEnvironment e, (FType, String) token)
        {
            var (t, v) = token;
            if (t == FType.FNull)
                return new FEnvironment(e.DataStack, e.WordDict, null, FMode.Halt);
            
            if (t == FType.FWord)
            {
                return e.WordDict[v](e);
            }
            else
            {
                e.DataStack.Push((t, v));
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode);
            }
        }

        public static (IEnumerable<string> tail, (FType, string) token) TokenizeHead(IEnumerable<string> input, FWordDict wordDict)
        {
            var head = input.First();
            var tail = input.Skip(1);
            var token = Tokenize(head, wordDict);
            return (tail, token);
        }

        public static ((FType, String) token, IEnumerable<string> tail) Read(IEnumerable<string> input,  Func<string> readLine, FWordDict wordDict)
        {
            if (!input.Any())
            {
                var nextLine = readLine();
                if (nextLine == null)
                    return ((FType.FNull, null), input);
                else if (nextLine.Trim() == "")
                    return Read(input, readLine, wordDict);
                else
                    return Read(nextLine.Trim().Split(), readLine, wordDict);
            }
            else
            {
                return (Tokenize(input.First(), wordDict), input.Skip(1));
            }
        }

        public static FEnvironment RunInterpreter(FEnvironment e, Func<string> readLine)
        {
            while (e.Mode != FMode.Halt)
            {
                var (token, input) = Read(e.Input, Console.ReadLine, e.WordDict);
                e = new FEnvironment(e.DataStack, e.WordDict, input, e.Mode);
                e = Eval(e, token);
            }

            return e;
        }

        public static void Main(string[] args)
        {
            var e = new FEnvironment(DataStack, WordDict, new List<string>(), FMode.Eval);
            RunInterpreter(e, Console.ReadLine);
        }
    }
}