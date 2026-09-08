using System;
using System.Buffers;

namespace UnifiProtectClient.Services;

public sealed class VideoFrame : IDisposable
{
    private readonly byte[] _pixels;
    private bool _disposed;

    public byte[] Pixels => _pixels;
    public int Width { get; }
    public int Height { get; }
    public int DataLength { get; }

    internal VideoFrame(byte[] pixels, int width, int height, int dataLength)
    {
        _pixels = pixels;
        Width = width;
        Height = height;
        DataLength = dataLength;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ArrayPool<byte>.Shared.Return(_pixels);
    }
}
