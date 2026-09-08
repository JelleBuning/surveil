using System;

namespace Surveil.Services;

internal interface IVlcPlayerHandle : IDisposable
{
    event EventHandler? Playing;
    event EventHandler? EncounteredError;
    event EventHandler? EndReached;
    event EventHandler<VideoFrame>? FrameReady;
    void Play();
    void Stop();
}
