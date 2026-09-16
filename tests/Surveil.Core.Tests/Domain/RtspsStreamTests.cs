using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Domain.Cameras;

namespace Surveil.Core.Tests.Domain;

[TestClass]
public sealed class RtspsStreamTests
{
    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        var stream = new RtspsStream("rtsps://host/stream", "high");

        Assert.AreEqual("rtsps://host/stream", stream.Url);
        Assert.AreEqual("high", stream.StreamName);
    }

    [TestMethod]
    public void Equality_ComparesByValue()
    {
        var stream = new RtspsStream("rtsps://host/stream", "high");
        var same = new RtspsStream("rtsps://host/stream", "high");
        var other = new RtspsStream("rtsps://host/other", "high");

        Assert.AreEqual(stream, same);
        Assert.AreNotEqual(stream, other);
    }
}
