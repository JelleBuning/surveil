using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class UnifiProtectOptionsTests
{
    [TestMethod]
    public void Options_Properties_AreSetViaInitializer()
    {
        var options = new UnifiProtectOptions
        {
            BaseUrl = "https://192.168.0.1/proxy/protect/api",
            ApiKey = "my-api-key"
        };

        Assert.AreEqual("https://192.168.0.1/proxy/protect/api", options.BaseUrl);
        Assert.AreEqual("my-api-key", options.ApiKey);
    }
}
