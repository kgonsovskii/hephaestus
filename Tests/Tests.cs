using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using cp;
using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using model;
using Newtonsoft.Json.Linq;
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