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
        /// Ensure that serializing/unserializing the environment and running an evaluation cycle
        /// produces the same environment as running an evaluation cycle without serialization.
        /// </summary>
        [Test]
        public void TestSerialization()
        {
            var initialEnvironment = new FEnvironment(new FStack(), BuiltinWords, new List<string>(), FMode.Execute, 0, null, new List<string>(), Console.WriteLine);
            var predefinedWordFile = new System.IO.StreamReader(@"PredefinedWords.forsch");
            var preloadedEnvironment = RunInterpreter(initialEnvironment, predefinedWordFile.ReadLine);
            predefinedWordFile.Close();

            var fs = File.Create("EnvTest.json");
            var writer = new Utf8JsonWriter(fs);
            var options = new JsonSerializerOptions();
            new EnvironmentConverter().Write(writer, preloadedEnvironment, options);
        }
    }
}