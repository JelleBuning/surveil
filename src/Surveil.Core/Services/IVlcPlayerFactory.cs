using System;

namespace Surveil.Services;

internal interface IVlcPlayerFactory
{
    IVlcPlayerHandle Create(string url, Action<string> onError);
}
