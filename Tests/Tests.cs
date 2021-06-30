using System;
using System.Collections.Generic;
using System.IO;
using Forcsh;
using NUnit.Framework;
using f = Forcsh.Program;


namespace Tests
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Action<Stack<(FType, String)>>>;
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestTokenizeHead()
        {
            var ss = new string[] {"5","5","+"};
            var e = new FEnvironment(new FStack(), f.WordDict, ss, true);
            var (tail, token) = f.TokenizeHead(e);
            Assert.AreEqual((FType.FInt, "5"), token);
            Assert.AreEqual(new List<string>(new string[] {"5", "+"}), new List<string>(tail));
            
            var ss2 = new string[] {"+", "."};
            var e2 = new FEnvironment(new FStack(), f.WordDict, ss2, true);
            var (_, token2) = f.TokenizeHead(e2);
            Assert.AreEqual((FType.FWord, "+"), token2);
        }
    }
}