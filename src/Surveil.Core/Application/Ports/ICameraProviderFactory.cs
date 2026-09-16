using Surveil.Application.Settings;

namespace Surveil.Application.Ports;

public interface ICameraProviderFactory
{
    VideoProviderType ProviderType { get; }

    ICameraProvider? Create(AppSettings settings);
}
