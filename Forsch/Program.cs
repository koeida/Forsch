using System;
using System.Collections.Generic;
using System.Linq;
using static Forsch.Builtins;
using static Forsch.Interpreter;

namespace Forsch
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Word>;

    public static class Program
    {
        /// <summary> /// Spins up a new Forsch interpreter on standard input
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var initialEnvironment = new FEnvironment(new FStack(), BuiltinWords, new List<string>(), FMode.Execute, 0, null, new List<string>());
            
            // Load up core premade words that didn't have to get written in C#
            var predefinedWordFile = new System.IO.StreamReader(@"PredefinedWords.forsch");
            var preloadedEnvironment = RunInterpreter(initialEnvironment, predefinedWordFile.ReadLine);
            predefinedWordFile.Close();

            // Spin up a fresh environment with the predefined words loaded in
            var e = new FEnvironment(preloadedEnvironment.DataStack, preloadedEnvironment.WordDict,
                new List<string>(), FMode.Execute, 0, null, null);
            RunInterpreter(e, Console.ReadLine);
        }
    }
}