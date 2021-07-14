using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Forsch;
using NUnit.Framework;
using static Forsch.Builtins;
using static Forsch.Interpreter;

namespace ForschTests
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Word>;
    [TestFixture]
    public class Tests
    {
        /// <summary>
        /// Ensure that a serialized/unserialized environment is equal to the original.
        /// </summary>
        [Test]
        public void TestSerialization()
        {
            var initialEnvironment = new FEnvironment(new FStack(), new FWordDict(BuiltinWords), new List<string>(), FMode.Execute, 0, null, new List<string>(), Console.WriteLine);
            var predefinedWordFile = new System.IO.StreamReader(@"PredefinedWords.forsch");
            var preloadedEnvironment = RunInterpreter(initialEnvironment, predefinedWordFile.ReadLine);
            predefinedWordFile.Close();
            
            var testInput = new StringReader("1 1 + .");
            preloadedEnvironment.Mode = FMode.Execute;
            var steppedEnvironment = StepEnvironment(preloadedEnvironment, testInput.ReadLine);

            var writer = new StreamWriter(@"EnvTest.json");
            SerializeEnvironment(steppedEnvironment, writer);
            writer.Close();
            
            var deserializedEnvironment = DeserializeEnvironment(new StreamReader(@"EnvTest.json"), Console.WriteLine);
            Console.WriteLine("here");
        }
    }
}