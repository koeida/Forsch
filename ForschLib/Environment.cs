using System;
using System.Collections.Generic;

namespace Forsch
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Word>;
    
    /// <summary>
    /// Internal representation of a word:
    /// a reference to a function that modifies the environment and
    /// a flag indicating whether the word is run in immediate mode.
    /// </summary>
    public class Word
    {
        public Func<FEnvironment, FEnvironment> WordFunc;
        public bool IsImmediate;
        public string[] WordText;

        public Word(Func<FEnvironment, FEnvironment> wordFunc, bool isImmediate, string[] wordText)
        {
            WordFunc = wordFunc;
            IsImmediate = isImmediate;
            WordText = wordText;
        }
    }
    /// <summary>
    /// Represents the different modes of the ForschLib interpreter.
    /// Halt indicates that the environment should halt its read/eval loop.
    /// Execute indicates that the environment should continue immediately interpreting each new word.
    /// Compile indicates the the environment should compile each new word instead of immediately executing it.
    /// </summary>
    public enum FMode
    {
        Halt,
        Execute,
        Compile
    };
    
    /// <summary>
    /// The basic set of types in ForschLib.
    /// FWord represents a ForschLib word (a subroutine reference)
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
    ///
    /// I wanted to use records for this but got tired of fighting with the IDE to let me use C# 9.
    /// </summary>
    public struct FEnvironment
    {
        /// <summary>
        /// "The Stack" -- the main Forth stack. Sometimes Forths use other stacks, but ForschLib has only one.
        /// </summary>
        public FStack DataStack;

        /// <summary>
        /// The dictionary containing all premade words, along with any user-defined words added at runtime.
        /// </summary>
        public FWordDict WordDict;

        /// <summary>
        /// Reference to the definition of current word being compiled
        /// </summary>
        public List<String> CurWordDef;

        /// <summary>
        /// Name of current word being compiled
        /// </summary>
        public String CurWord;

        /// <summary>
        /// The list of words to be evaluated.
        /// </summary>
        public List<String> Input;

        /// <summary>
        /// The current mode, as described in the enum above.
        /// </summary>
        public FMode Mode;

        /// <summary>
        /// Current index of next word to consume.
        /// </summary>
        public int InputIndex;

        public Action<string> WriteLine;
        
        public FEnvironment(FStack dataStack, FWordDict wordDict, List<string> input,
            FMode mode, int inputIndex, string curWord, List<string> curWordDef, Action<string> writeLine)
        {
            DataStack = dataStack;
            WordDict = wordDict;
            Input = input;
            InputIndex = inputIndex;
            Mode = mode;
            CurWordDef = curWordDef;
            CurWord = curWord;
            WriteLine = writeLine;
        }
    }
}