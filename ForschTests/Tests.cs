using System;
using NUnit.Framework;

namespace ForschTests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void CompileTests()
        {
            var preCompiled = "BLAH BLAH BEGIN 1 + DUP 10 = UNTIL";
            //Assert.AreEqual("BLAH BLAH BEGIN 1 + DUP 10 = BRANCHF 2");
        }
    }
}