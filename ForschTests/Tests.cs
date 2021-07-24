using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [Test]
        public void TestPick()
        {
            var testInput = "a b c 2 PICK";
            var initialEnvironment = FEnvFactory(testInput); 

            var step1 = StepEnvironment(initialEnvironment);
            var step2 = StepEnvironment(step1);
            var step3 = StepEnvironment(step2);
            var step4 = StepEnvironment(step3);
            var step5 = StepEnvironment(step4);
            Assert.AreEqual(3, step5.DataStack.Count);
            Assert.AreEqual("a", step5.DataStack.Pop().Item2);
        }

        /// <summary>
        /// New words compiled to dictionary stepwise
        /// instead of all at once upon completion.
        /// </summary>
        [Test]
        public void TestNewWord()
        {
            var testInput = ": ADD1 IMMEDIATE 1 + ;";
            var initialEnvironment = FEnvFactory(testInput); 

            var step1 = StepEnvironment(initialEnvironment);
            Assert.AreEqual(new List<string>(), step1.WordDict["ADD1"].WordText);

            var step2 = StepEnvironment(step1);
            Assert.AreEqual(new List<string>(), step2.WordDict["ADD1"].WordText);
            
            var step3 = StepEnvironment(step2);
            Assert.AreEqual(new List<string>(new string[]{"1"}), step3.WordDict["ADD1"].WordText);
        }
        
        /// <summary>
        /// Output directed to environment.Output every step.
        /// </summary>
        [Test]
        public void TestOutput()
        {
            var testInput = "hello_world .";
            var initialEnvironment = FEnvFactory(testInput);

            var steppedEnvironment = StepEnvironment(initialEnvironment);
            Assert.AreEqual("", steppedEnvironment.Output);

            var env2 = StepEnvironment(steppedEnvironment);
            Assert.AreEqual("hello_world", env2.Output);
            
            var serializedEnvironment = SerializeEnvironment(env2);
            Assert.IsTrue(serializedEnvironment.Contains("\"Output\":\"hello_world\""));
        }
        
        /// <summary>
        /// Test that compilation can proceed stepwise, appending words
        /// to the CurWord buffer until the word ends.
        /// </summary>
        [Test]
        public void TestCompileMode()
        {
            var testInput = ": ADD1 IMMEDIATE 1 + ;";
            var initialEnvironment = FEnvFactory(testInput);
            Func<FEnvironment, string> curWord = (FEnvironment e) => String.Join(" ", e.WordDict[e.CurWord].WordText);

            var steppedEnvironment = StepEnvironment(initialEnvironment);

            Assert.AreEqual(FMode.Compile, steppedEnvironment.Mode);
            Assert.AreEqual("ADD1", steppedEnvironment.CurWord);
            Assert.AreEqual("IMMEDIATE", steppedEnvironment.Input[steppedEnvironment.InputIndex]);

            var step2 = StepEnvironment(steppedEnvironment);
            Assert.AreEqual(FMode.Compile, step2.Mode);
            Assert.AreEqual("1", step2.Input[step2.InputIndex]);
            Assert.IsTrue(step2.WordDict["ADD1"].IsImmediate);
            
            var step3 = StepEnvironment(step2);
            Assert.AreEqual("1", curWord(step3));
            
            var step4 = StepEnvironment(step3);
            Assert.AreEqual(";", step4.Input[step4.InputIndex]);
            Assert.AreEqual("1 +", curWord(step4));
            
            var finalStep = StepEnvironment(step4);

            Assert.AreEqual("1 +", String.Join(" ", finalStep.WordDict["ADD1"].WordText));
        }

        /// <summary>
        /// Ensure that a serialized/unserialized environment is equal to the original.
        /// </summary>
        [Test]
        public void TestSerialization()
        {
            var testInput = "1 1 + .";
            var steppedEnvironment = StepEnvironment(FEnvFactory(testInput));

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
            RunInterpreter(preloadedEnvironment);

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

        public static Func<string, FEnvironment> FEnvFactory = (string initialInput) => new FEnvironment(
            new FStack(), new FWordDict(BuiltinWords), initialInput.Split().ToList(),
            FMode.Execute, 0, null, new StringBuilder());

        private static FEnvironment LoadTestEnvironment(Action<string> outputHandler)
        {
            var initialEnvironment = new FEnvironment(new FStack(), new FWordDict(BuiltinWords), new List<string>(),
                FMode.Execute, 0, null, new StringBuilder());
            var predefinedWordFile = new System.IO.StreamReader(@"PredefinedWords.forsch");
            var preloadedEnvironment = RunInterpreter(initialEnvironment);
            predefinedWordFile.Close();
            preloadedEnvironment.Mode = FMode.Execute;
            return preloadedEnvironment;
        }
    }
}