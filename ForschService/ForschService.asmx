<%@ WebService Language="C#" Class="Forsch.ForschService" %>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Services;
using System.Web.Services;

using static Forsch.Builtins;
using static Forsch.Interpreter;

namespace Forsch
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Word>;
    
    [WebService (Namespace="http://127.0.0.1:9000/ForschService")]
    [ScriptService]
    public class ForschService : WebService
    {
        public string output;

        public void writeOutput(string s)
        {
            output += s + "\n";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string EvalStep(string jsonEnvironment)
        {
            var output = new StringBuilder();
            var e = DeserializeEnvironment(jsonEnvironment, (s) => output.Append(s));
            var newEnvironment = StepEnvironment(e);
            return SerializeEnvironment(newEnvironment);
        }
        
        [WebMethod]
        public string Evaluate(string code)
        {
            output = "";
            var inputStream = new System.IO.StringReader(code);
            
            var initialEnvironment = new FEnvironment(new FStack(), BuiltinWords, new List<string>(), FMode.Execute, 0, null, "");
                        
            // Load up core premade words that didn't have to get written in C#
            var predefinedWordFile = new System.IO.StreamReader(@"PredefinedWords.forsch");
            var preloadedEnvironment = RunInterpreter(initialEnvironment, predefinedWordFile.ReadLine);
            predefinedWordFile.Close();

            // Spin up a fresh environment with the predefined words loaded in
            var e = new FEnvironment(preloadedEnvironment.DataStack, preloadedEnvironment.WordDict,
                new List<string>(), FMode.Execute, 0, null, "");
            RunInterpreter(e, inputStream.ReadLine);
            return output;
        }
    }
}