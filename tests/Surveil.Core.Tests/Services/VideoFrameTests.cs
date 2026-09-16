using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestClass]
public sealed class VideoFrameTests
{
    private static VideoFrame RentFrame(int width = 4, int height = 4)
    {
        var dataLength = width * height * 4;
        var pixels = ArrayPool<byte>.Shared.Rent(dataLength);
        return new VideoFrame(pixels, width, height, dataLength);
    }

    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        using var frame = RentFrame(8, 6);

        Assert.AreEqual(8, frame.Width);
        Assert.AreEqual(6, frame.Height);
        Assert.AreEqual(8 * 6 * 4, frame.DataLength);
        Assert.IsNotNull(frame.Pixels);
    }

    [TestMethod]
    public void Dispose_ReturnsPixelsToThePool()
    {
        var frame = RentFrame();

        frame.Dispose();
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var frame = RentFrame();

        frame.Dispose();
        frame.Dispose();
    }
}
