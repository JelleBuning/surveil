using System;
using Microsoft.Extensions.Options;
using Surveil.Application.Ports;
using Surveil.Application.Settings;

namespace Surveil.Unifi;

public sealed class UnifiCameraProviderFactory : ICameraProviderFactory
{
    public VideoProviderType ProviderType => VideoProviderType.UnifiProtect;

    public ICameraProvider? Create(AppSettings settings)
    {
        if (!Uri.IsWellFormedUriString(settings.UnifiProtect.BaseUrl, UriKind.Absolute))
            return null;

        return new UnifiProtectApiClient(Options.Create(new UnifiProtectOptions
        {
            BaseUrl = settings.UnifiProtect.BaseUrl,
            ApiKey  = settings.UnifiProtect.ApiKey
        }));
    }
}
