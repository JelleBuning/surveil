using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Surveil.Services;

[ExcludeFromCodeCoverage]
internal sealed class DefaultVlcPlayerFactory : IVlcPlayerFactory
{
    private static readonly LibVLC Shared = CreateShared();

    private static LibVLC CreateShared()
    {
        var arch = Environment.Is64BitProcess ? "win-x64" : "win-x86";
        var vlcDir = Path.Combine(AppContext.BaseDirectory, "libvlc", arch);
        Core.Initialize(vlcDir);

        var libVlc = new LibVLC(enableDebugLogs: false);

        libVlc.Log += (_, args) =>
        {
            if (args.Level == LogLevel.Error)
                Debug.WriteLine($"[VLC Error] {args.FormattedLog}");
        };

        return libVlc;
    }

    public IVlcPlayerHandle Create(string url, Action<string> onError) =>
        new VlcPlayerHandle(Shared, url, onError);
}
