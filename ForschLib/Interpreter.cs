using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;
using static Forsch.Builtins;

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
                var tempEnv = new FEnvironment(e.DataStack, e.WordDict, wordData, e.Mode, 0, e.CurWord, e.Output);
                
                var resultEnv = RunInterpreter(tempEnv);

                return new FEnvironment(resultEnv.DataStack, resultEnv.WordDict, e.Input, FMode.Execute, e.InputIndex,
                    e.CurWord, resultEnv.Output);
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
        /// Evaluates the next token on the stream.
        /// 
        /// Evaluation depends on what mode the interpreter is in,
        /// compile mode or execute mode.
        /// 
        /// In compile mode, we're appending words onto CurWordDef
        /// unless the words are flagged as immediate words. Immediate
        /// words are executed immediately as if it were execute mode,
        /// after which we return to compile mode. Clever use of compile
        /// mode with immediate words allows us to define a large number
        /// of control structures (e.g, IF/THEN) in forth itself without
        /// having to define them in the interpreter.
        ///
        /// In execute mode, we immediately push tokens onto the stack
        /// unless they represent words defined in the word dictionary, in which case
        /// we jump to that word definition and begin evaluation on it.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="token">THe next token on the stream</param>
        /// <returns>Modified environment</returns>
        public static FEnvironment Eval(FEnvironment e, (FType, String) token)
        {
            var (t, v) = token;
            
            //Null token received: We're done evaluating this input stream.
            if (t == FType.FNull)
            {
                e.Mode = FMode.Halt;
                return e;
            }

            if (e.Mode == FMode.Compile)
            {
                if (t == FType.FWord && e.WordDict[v].IsImmediate)
                {
                    var oldMode = e.Mode;
                    e.Mode = FMode.Execute;
                    var result = e.WordDict[v].WordFunc(e);
                    result.Mode = oldMode;
                    
                    return result;
                }
                else
                {
                    e.WordDict[e.CurWord].WordText.Add(v);
                    return e;
                }
            }
            else if (e.Mode == FMode.Execute)
            {
                if (t == FType.FWord)
                    return e.WordDict[v].WordFunc(e);
                else
                {
                    e.DataStack.Push((t, v));
                    return e;
                }
            }
            else
                throw new Exception("Mode error: Somehow we're in eval but neither executing nor compiling.");
        }

        /// <summary>
        /// Tokenizes the next word from the current line.
        /// If the current line has been completely tokenized, grab a new line from the input stream.
        /// Null token returned if stream empty.
        /// </summary>
        /// <param name="input">Current list of untokenized words</param>
        /// <param name="inputIndex"></param>
        /// <param name="wordDict">The ForschLib word dictionary</param>
        /// <returns></returns>
        public static ((FType, string) token, List<string> input, int newIndex) Read(List<string> input, int inputIndex,
            FWordDict wordDict)
        {
            if (inputIndex >= input.Count())
                return ((FType.FNull, null), input, 0);
            else
                return (Tokenize(input[inputIndex], wordDict), input, inputIndex + 1);
        }

        /// <summary>
        /// Given a ForschLib environment and a function to read lines from the stream,
        /// this begins an evaluation loop on that stream until it is complete,
        /// returning the new ForschLib environment.
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="readLine">Function to grab a line from a stream</param>
        /// <returns>New environment</returns>
        public static FEnvironment RunInterpreter(FEnvironment e)
        {
            while (e.Mode != FMode.Halt)
                e = StepEnvironment(e);

            return e;
        }

        public static FEnvironment StepEnvironment(FEnvironment e)
        {
            var (token, input, newIndex) = Read(e.Input, e.InputIndex, e.WordDict);
            e = new FEnvironment(e.DataStack, e.WordDict, input, e.Mode, newIndex, e.CurWord, e.Output);
            e = Eval(e, token);
            return e;
        }

        public static FEnvironment DeserializeEnvironment(string jText, Action<string> writeLine)
        {
            JObject jEnv;
            try
            {
                jEnv = JsonConvert.DeserializeObject<JObject>(jText);
            }
            catch(Exception e)
            {
              Console.WriteLine(jText);
              throw new Exception("Error deserializing: " + e.Message);
            }
            
            //Build data stack from JSON
            var stackList = jEnv["DataStack"]
                .Select(t =>
                {
                    var type = (FType) Enum.Parse(typeof(FType), t["type"].ToString());
                    var value = t["value"].ToString();
                    return (type, value);
                });
            var stack = new FStack(stackList);

            // Build word dictionary combining builtin words with 
            // user-defined words in json
            var words = new FWordDict(BuiltinWords);
            foreach (var jToken in jEnv["WordDict"])
            {
                var wordText = jToken["WordText"].Select(t => t.ToString());
                var wordName = jToken["WordName"].ToString();
                var isImmediate = bool.Parse(jToken["IsImmediate"].ToString());
                words.Add(wordName, new Word(WordWrapper(wordText.ToList()), isImmediate, wordText.ToList()));
            }

            //Build remaining environment variables from JSON
            var input = jEnv["Input"].Select(t => t.ToString()).ToList();
            var inputIndex = Convert.ToInt16(jEnv["InputIndex"].ToString());
            var mode = (FMode) Enum.Parse(typeof(FMode), jEnv["mode"].ToString());
            var curWord = jEnv["CurWord"].Type == JTokenType.Null || jEnv["CurWord"].Type == JTokenType.None
                ? null
                : jEnv["CurWord"].ToString();

            return new FEnvironment(stack, words, input, mode, inputIndex, curWord, new StringBuilder());
        }
        
        public static string SerializeEnvironment(FEnvironment e)
        {
            //Only serialize words that are user-defined.
            var words = e
                .WordDict
                .Where(e => e.Value.WordText != null)
                .Select(e => new Dictionary<string, object>
                {
                    {"WordName", e.Key},
                    {"IsImmediate", e.Value.IsImmediate},
                    {"WordText", e.Value.WordText}
                })
                .ToList();
            
            var stack = e
                .DataStack
                .Select(v => new Dictionary<string, string> {{"type", v.Item1.ToString()}, {"value", v.Item2}})
                .Reverse();
            
            var EnvDict = new Dictionary<string,object>
            {
                {"DataStack", stack}, 
                {"WordDict", words},
                {"Input", e.Input},
                {"InputIndex", e.InputIndex},
                {"mode", e.Mode.ToString()},
                {"CurWord", e.CurWord},
                {"Output", e.Output.ToString()}
            };
            
            return JsonSerializer.Serialize(EnvDict);
        }

        /// <summary>
        /// Takes a json string containing a serialized environment,
        /// deserializes it, runs one eval step, and re-serializes it.
        /// </summary>
        /// <param name="outputHandler">Handler for any output produced during the evaluation step</param>
        /// <returns>New serialized environment</returns>
        public static string StepJsonEnvironment(string jsonInput, Func<String> inputHandler, Action<string> outputHandler)
        {
            var deserializedEnvironment = DeserializeEnvironment(jsonInput, outputHandler);
            
            var newEnvironment = StepEnvironment(deserializedEnvironment);

            return SerializeEnvironment(newEnvironment);
        }
    }
}