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

        public Word(Func<FEnvironment, FEnvironment> wordFunc, bool isImmediate)
        {
            WordFunc = wordFunc;
            IsImmediate = isImmediate;
        }
    }
    /// <summary>
    /// Represents the different modes of the Forsch interpreter.
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
        /// Reference to the definition of current word being compiled
        /// </summary>
        public List<String> CurWordDef { get; }

        /// <summary>
        /// Name of current word being compiled
        /// </summary>
        public String CurWord { get; }
        
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
        
        public FEnvironment(FStack dataStack, FWordDict wordDict, List<string> input, 
            FMode mode, int inputIndex, string curWord, List<string> curWordDef)
        {
            DataStack = dataStack;
            WordDict = wordDict;
            Input = input;
            InputIndex = inputIndex;
            Mode = mode;
            CurWordDef = curWordDef;
            CurWord = curWord;
        }
    }
}