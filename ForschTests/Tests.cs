using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ForcshTests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void CompileTests()
        {
            var replacements = new Dictionary<String, String>
            {
                ["alpha"] = "beta",
                ["gamma"] = "delta"
            };
            Assert.AreEqual("beta 1 delta 2", Forsch.Program.DictionaryReplace(replacements, "alpha 1 gamma 2"));
            Assert.AreEqual("zeta eta theta", Forsch.Program.DictionaryReplace(replacements, "zeta eta theta"));
            //var preCompiled = "BLAH BLAH BEGIN 1 + DUP 10 = UNTIL";
            //Assert.AreEqual("BLAH BLAH BEGIN 1 + DUP 10 = BRANCHF 2", Forsch.Program.Compile(preCompiled));
        }
    }
}