using System.Collections;
using System.Collections.Generic;
using Language;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LanguageFirstTests
{
    // todo load text files from resources
    /*
 if (true)
{
	print "yay";
}

if (false)
{
	print "boooo";
}
else
	print "yayyy!";
	
var cow = "living";
if (cow)
{
	print "eat veggies";
}
     */
    // A Test behaves as an ordinary method
    [Test]
    public void LanguageFirstTestsSimplePasses()
    {
        Assert.That(()=> LangName.Run("if ((true){print \"Hit\";}"), Throws.Exception);
        // Use the Assert class to test conditions
    }

    [Test]
    public void ValidIfConditional_Test()
    {
        var str = "if (true || false)" +
                  "{" +
                  "    print \"good\";" +
                  "}";
        
        LangName.Run(str);
        // will not throw 
    }
    // LangName.Run("if (false || true)\n{\nprint \"Gooood\"\n}")
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator LanguageFirstTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
