using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Domain.Cameras;

namespace Surveil.Core.Tests.Domain;

[TestClass]
public sealed class CameraTests
{
    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        var camera = new Camera("id-1", "Front Door", true);

        Assert.AreEqual("id-1", camera.Id);
        Assert.AreEqual("Front Door", camera.Name);
        Assert.IsTrue(camera.IsConnected);
    }

    [TestMethod]
    public void Constructor_WithIsConnectedFalse_LeavesTheCameraDisconnected()
    {
        var camera = new Camera("id-2", "Backyard", false);

        Assert.IsFalse(camera.IsConnected);
    }

    [TestMethod]
    public void Equality_ComparesByValue()
    {
        var camera = new Camera("id-1", "Front Door", true);
        var same = new Camera("id-1", "Front Door", true);
        var other = new Camera("id-2", "Front Door", true);

        Assert.AreEqual(camera, same);
        Assert.AreNotEqual(camera, other);
    }

    [TestMethod]
    public void ToString_ContainsTheFieldValues()
    {
        var camera = new Camera("abc", "Garage", false);

        var text = camera.ToString();

        Assert.IsNotNull(text);
        Assert.IsTrue(text.Contains("abc"));
    }
}
