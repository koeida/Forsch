using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var preloadedEnvironment = LoadTestEnvironment(Console.WriteLine);

            //Run one bit of test input
            var testInput = new StringReader("1 1 + .");
            var steppedEnvironment = StepEnvironment(preloadedEnvironment, testInput.ReadLine);

            //Serialize test environment
            var serializedEnvironment = SerializeEnvironment(steppedEnvironment);
            
            //Compare serialized/deserialized to original.
            var deserializedEnvironment = DeserializeEnvironment(serializedEnvironment, Console.WriteLine);
            Assert.AreEqual(steppedEnvironment, deserializedEnvironment);
        }
        
        /// <summary>
        /// Run a simple computation with the normal read/eval loop and compare it to
        /// a computation run by repeatedly serializing/deserializing and stepping through
        /// the evaluation one word at a time. Passes if output is the same.
        /// </summary>
        [Test]
        public void TestStepJsonEnvironment()
        {
            var testCode = "1 2 SWAP 3 4 SWAP . . . .";
            
            var envOutput1 = new StringBuilder();
            var preloadedEnvironment = LoadTestEnvironment(s => envOutput1.Append(s));
            var testInput = new StringReader(testCode);
            RunInterpreter(preloadedEnvironment, testInput.ReadLine);

            var (serializedEnvironment, testInput2, envOutput2) = InitializeSerializedEnvironment(testCode);
            
            while (true)
            {
                serializedEnvironment = StepJsonEnvironment(serializedEnvironment, testInput2.ReadLine, (s) => envOutput2.Append(s));
                var e = DeserializeEnvironment(serializedEnvironment, (s) => envOutput2.Append(s));
                if (e.Mode == FMode.Halt)
                    break;
            }
            Assert.AreEqual(envOutput1.ToString(), envOutput2.ToString());

        }

        private static (string, StringReader, StringBuilder) InitializeSerializedEnvironment(string testCode)
        {
            var envOutput = new StringBuilder();
            var preloadedEnvironment = LoadTestEnvironment(s => envOutput.Append(s));
            var testInput = new StringReader(testCode);
            var serializedEnvironment = SerializeEnvironment(preloadedEnvironment);
            return (serializedEnvironment, testInput, envOutput);
        }

        private static FEnvironment LoadTestEnvironment(Action<string> outputHandler)
        {
            var initialEnvironment = new FEnvironment(new FStack(), new FWordDict(BuiltinWords), new List<string>(),
                FMode.Execute, 0, null, new List<string>(), outputHandler);
            var predefinedWordFile = new System.IO.StreamReader(@"PredefinedWords.forsch");
            var preloadedEnvironment = RunInterpreter(initialEnvironment, predefinedWordFile.ReadLine);
            predefinedWordFile.Close();
            preloadedEnvironment.Mode = FMode.Execute;
            return preloadedEnvironment;
        }
    }
}