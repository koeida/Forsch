using System;
using System.Collections.Generic;
using System.Linq;

namespace Forsch
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Word>;
    
    public static class Interpreter
    {
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
                var tempEnv = new FEnvironment(e.DataStack, e.WordDict, wordData, e.Mode, 0, e.CurWord, e.CurWordDef);
                
                var resultEnv = RunInterpreter(tempEnv, () => null);

                return new FEnvironment(resultEnv.DataStack, resultEnv.WordDict, e.Input, FMode.Execute, e.InputIndex, e.CurWord, resultEnv.CurWordDef);
            };
        }

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
            {
                e.Mode = FMode.Halt;
                return e;
            }

            // In compile mode, words are just appended onto CurWordDef one at a time
            // unless they're immediate words.
            // Immediate words are immediately executed even though we're in compile mode.
            if (e.Mode == FMode.Compile)
            {
                if (t == FType.FWord && e.WordDict[v].IsImmediate)
                {
                    //Extremely verbose way of switching a single boolean. Change this.
                    e.Mode = FMode.Execute;
                    var result = e.WordDict[v].WordFunc(e);
                    result.Mode = FMode.Compile;
                    return result;
                }
                else
                {
                    e.CurWordDef.Add(v);
                    return e;
                }
            }
            else if (e.Mode == FMode.Execute)
            {
                // If it's a word, execute it. If it's not a word, push it onto the stack.
                if (t == FType.FWord)
                {
                    return e.WordDict[v].WordFunc(e);
                }
                else
                {
                    e.DataStack.Push((t, v));
                    return e;
                }
            }
            else
            {
                throw new Exception("Mode error: Somehow we're in eval but neither executing nor compiling.");
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
        public static ((FType, String) token, List<string> input, int newIndex) Read(List<string> input, int inputIndex, Func<string> readLine, FWordDict wordDict)
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
                var (token, input, newIndex) = Read(e.Input, e.InputIndex, readLine, e.WordDict);
                e = new FEnvironment(e.DataStack, e.WordDict, input, e.Mode, newIndex, e.CurWord, e.CurWordDef);
                e = Eval(e, token);
            }

            return e;
        }
    }
}