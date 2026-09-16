using System;
using System.Buffers;

namespace Surveil.Services;

public sealed class VideoFrame : IDisposable
{
    private bool _disposed;

    public byte[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    public int DataLength { get; }

    internal VideoFrame(byte[] pixels, int width, int height, int dataLength)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        DataLength = dataLength;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ArrayPool<byte>.Shared.Return(Pixels);
    }
}
