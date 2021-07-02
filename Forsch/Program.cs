using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace Forsch
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Func<FEnvironment, FEnvironment>>;
    
    /// <summary>
    /// Represents the different modes of the Forsch interpreter. Usually Forths switch between
    /// immediate mode and compile mode, but this one works a little different.
    /// Halt indicates that the environment should stop interpreting.
    /// Eval indicates that the environment should continue interpreting.
    /// </summary>
    public enum FMode
    {
        Halt,
        Eval
    };
    
    /// <summary>
    /// The basic set of types in Forsch.
    /// FWord represents a Forsch word (a subroutine reference)
    /// An FNull token is used to indicate that the program or subroutine should terminate:
    /// It's pushed onto the stack when the input stream is empty.
    /// The rest map onto C# types.
    /// </summary>
    public enum FType
    {
        FStr,
        FFloat,
        FInt,
        FWord,
        FNull,
        FBool
    }
    
    /// <summary>
    /// Holds all the information about the current execution context,
    /// passed around to and from almost every function.
    /// It was written as readonly with the intention of making it immutable
    /// for ease of debugging and testing.
    ///
    /// In C# 9 I would use records for this, since that's what I'm poorly imitating here throughout.
    /// </summary>
    public readonly struct FEnvironment
    {
        /// <summary>
        /// "The Stack" -- the main Forth stack. Sometimes Forths use other stacks, but Forsch has only one.
        /// </summary>
        public FStack DataStack { get; }
        /// <summary>
        /// The dictionary containing all premade words, along with any user-defined words added at runtime.
        /// </summary>
        public FWordDict WordDict { get; }
        /// <summary>
        /// The list of words to be evaluated.
        /// </summary>
        public List<String> Input { get; }
        /// <summary>
        /// The current mode, as described in the enum above.
        /// </summary>
        public FMode Mode { get; }
        
        /// <summary>
        /// Current index of next word to consume.
        /// </summary>
        public int InputIndex { get; }
        
        public FEnvironment(FStack dataStack, FWordDict wordDict, List<string> input, FMode mode, int inputIndex)
        {
            DataStack = dataStack;
            WordDict = wordDict;
            Input = input;
            InputIndex = inputIndex;
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
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
            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
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

            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
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

            var res = "";
            if (xt == FType.FInt)
                res = (System.Convert.ToInt32(xv) + System.Convert.ToInt32(yv)).ToString();
            else if (xt == FType.FFloat)
                res = $"{System.Convert.ToSingle(xv) + System.Convert.ToSingle(yv):0.0000}";
            else if (xt == FType.FStr)
                res = xv + yv;
            else
                throw new Exception($"Unable to add value of type {xt}");
            s.Push((xt, res));
            
            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
        }

        public static FEnvironment FMult(FEnvironment e)
        {   
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
            if (xt != yt)
                throw new Exception($"Unable to multiple ({xt},{xv}) and ({yt}, {yv}): Type mismatch");

            var res = "";
            if (xt == FType.FInt)
                res = (System.Convert.ToInt32(xv) * System.Convert.ToInt32(yv)).ToString();
            else if (xt == FType.FFloat)
                res = $"{System.Convert.ToSingle(xv) * System.Convert.ToSingle(yv):0.0000}";
            else
                throw new Exception(($"Can't multiply value of type {xt}"));
                
            s.Push((xt, res));
            
            return new FEnvironment(s, WordDict, e.Input, e.Mode, e.InputIndex);
            
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
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, newIndex);
            else
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex);
        }

        /// <summary>
        /// Shifts the input index to the  indicated by the next word in the input 
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New Environment</returns>
        public static FEnvironment FBranch(FEnvironment e)
        {  
            var offset = ReadIntParam(e.Input, e.InputIndex);
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, offset);
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

            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, newIndex + 1);
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
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex);
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
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex);
        }

        /// <summary>
        /// Pushes the current e.InputIndex to the stack
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FHere(FEnvironment e)
        {
            e.DataStack.Push((FType.FInt, (e.InputIndex - 1).ToString()));
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex);
        }

        /// <summary>
        /// Pops a string and an index off the stack
        /// and then inserts that string into e.Input at that index.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>New environment</returns>
        public static FEnvironment FStore(FEnvironment e)
        {
            var (_,s) = e.DataStack.Pop();
            var (_,i) = e.DataStack.Pop();
            e.Input.Insert(Convert.ToInt32(i), s);
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex);
        }
        
        /// <summary>
        /// Takes a dictionary of keys to locate, and replaces them with corresponding
        /// values in string s.
        /// </summary>
        /// <param name="s">String to run search/replace on</param>
        /// <returns>New converted string</returns>
        public static String DictionaryReplace(Dictionary<String, String> replacementDict, string s)
        {
            var expandedLine = new StringBuilder(s);
            foreach (var keyValue in replacementDict)
                expandedLine.Replace(keyValue.Key, keyValue.Value);
            return expandedLine.ToString();
        }

        /// <summary>
        /// Changes the mode to halt
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <returns>The same environment</returns>
        public static FEnvironment FEndWord(FEnvironment e)
        {
            return new FEnvironment(e.DataStack, e.WordDict, e.Input, FMode.Halt, e.InputIndex);
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
        public static Func<FEnvironment, FEnvironment> WordWrapper(List<String> wordData)
        {
            return (FEnvironment e) =>
            {
                var tempEnv = new FEnvironment(e.DataStack, e.WordDict, wordData, e.Mode, 0);
                
                var resultEnv = RunInterpreter(tempEnv, () => null);

                return new FEnvironment(resultEnv.DataStack, resultEnv.WordDict, e.Input, FMode.Eval, e.InputIndex);
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
            var wordName = e.Input[e.InputIndex];
            var wordData = e.Input.Skip(2).ToList();
            e.WordDict[wordName] = WordWrapper(wordData);

            return new FEnvironment(e.DataStack, e.WordDict, new List<String>(), e.Mode, e.InputIndex);
        }

        /// <summary>
        /// Built-in words.
        /// </summary>
        public static FWordDict WordDict = new FWordDict
        {
            ["+"] = FAdd,
            ["*"] = FMult,
            ["."] = FDot,
            ["="] = FEq,
            [":"] = FWord,
            [";"] = FEndWord,
            ["DUP"] = FDup,
            ["DROP"] = FDrop,
            ["ASSERT"] = FAssert,
            ["SWAP"] = FSwap,
            ["BRANCH"] = FBranch,
            ["BRANCHF"] = FBranchOnFalse,
            ["SURVEY"] = FSurvey,
            ["EMPTY?"] = FIsEmpty,
            ["HERE"] = FHere,
            ["!"] = FStore,
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
                    return (FType.FBool, b.ToString());
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
                return new FEnvironment(e.DataStack, e.WordDict, null, FMode.Halt, e.InputIndex);
            
            if (t == FType.FWord)
            {
                return e.WordDict[v](e);
            }
            else
            {
                e.DataStack.Push((t, v));
                return new FEnvironment(e.DataStack, e.WordDict, e.Input, e.Mode, e.InputIndex);
            }
        }

        /// <summary>
        /// Tokenizes the next word from the current line.
        /// If the current line has been completely tokenized, grab a new line from the input stream.
        /// Null token returned if stream empty.
        /// </summary>
        /// <param name="input">Current list of untokenized words</param>
        /// <param name="readLine">Function to grab a new line from a stream</param>
        /// <param name="wordDict">The Forsch word dictionary</param>
        /// <returns></returns>
        public static ((FType, String) token, List<string> input, int newIndex) Read(List<string> input, int inputIndex,  Func<string> readLine, FWordDict wordDict)
        {
            if (inputIndex >= input.Count())
            {
                var nextLine = readLine();
                if (nextLine == null)
                    return ((FType.FNull, null), input, 0);
                else if (nextLine.Trim() == "")
                    return Read(new List<string>(), 0 , readLine, wordDict);
                else
                    return Read(new List<string>(nextLine.Trim().Split()), 0, readLine, wordDict);
            }
            else
            {
                return (Tokenize(input[inputIndex], wordDict), input, inputIndex + 1);
            }
        }

        /// <summary>
        /// Given a Forsch environment and a function to read lines from the stream,
        /// this begins an evaluation loop on that stream until it is complete,
        /// returning the new Forsch environment.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="readLine">Function to grab a line from a stream</param>
        /// <returns>New environment</returns>
        public static FEnvironment RunInterpreter(FEnvironment e, Func<string> readLine)
        {
            while (e.Mode != FMode.Halt)
            {
                var (token, input, newIndex) = Read(e.Input, e.InputIndex, Console.ReadLine, e.WordDict);
                e = new FEnvironment(e.DataStack, e.WordDict, input, e.Mode, newIndex);
                e = Eval(e, token);
            }

            return e;
        }
        /// <summary> /// Spins up a new Forsch interpreter on standard input
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var e = new FEnvironment(DataStack, WordDict, new List<string>(), FMode.Eval, 0);
            RunInterpreter(e, Console.ReadLine);
        }
    }
}