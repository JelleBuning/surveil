using System;

namespace UnifiProtectClient.Services;

internal interface IVlcPlayerFactory
{
    IVlcPlayerHandle Create(string url, Action<string> onError);
}
