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
        }
    }
}