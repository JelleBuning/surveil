using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Domain.Cameras;

namespace Surveil.Core.Tests.Domain;

[TestClass]
public sealed class CameraTests
{
    [TestMethod]
    public void Camera_Constructor_SetsAllProperties()
    {
        // Arrange & Act
        var camera = new Camera("id-1", "Front Door", true);

        // Assert
        Assert.AreEqual("id-1", camera.Id);
        Assert.AreEqual("Front Door", camera.Name);
        Assert.IsTrue(camera.IsConnected);
    }

    [TestMethod]
    public void Camera_WithIsConnectedFalse_ReturnsCorrectValue()
    {
        var camera = new Camera("id-2", "Backyard", false);
        Assert.IsFalse(camera.IsConnected);
    }

    [TestMethod]
    public void Camera_EqualityByValue_WorksCorrectly()
    {
        var a = new Camera("id-1", "Front Door", true);
        var b = new Camera("id-1", "Front Door", true);
        var c = new Camera("id-2", "Front Door", true);

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }

    [TestMethod]
    public void Camera_ToString_ContainsFields()
    {
        var camera = new Camera("abc", "Garage", false);
        var str = camera.ToString();
        Assert.IsNotNull(str);
        Assert.IsTrue(str.Contains("abc"));
    }
}

[TestClass]
public sealed class RtspsStreamTests
{
    [TestMethod]
    public void RtspsStream_Constructor_SetsProperties()
    {
        var stream = new RtspsStream("rtsps://host/stream", "high");
        Assert.AreEqual("rtsps://host/stream", stream.Url);
        Assert.AreEqual("high", stream.StreamName);
    }

    [TestMethod]
    public void RtspsStream_EqualityByValue_WorksCorrectly()
    {
        var a = new RtspsStream("rtsps://host/stream", "high");
        var b = new RtspsStream("rtsps://host/stream", "high");
        var c = new RtspsStream("rtsps://host/other", "high");

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }
}
