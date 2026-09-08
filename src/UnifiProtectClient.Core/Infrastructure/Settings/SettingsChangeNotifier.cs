using System;
using UnifiProtectClient.Application.Ports;
using UnifiProtectClient.Application.Settings;

namespace UnifiProtectClient.Infrastructure.Settings;

public sealed class SettingsChangeNotifier : ISettingsChangeNotifier
{
    public event Action<AppSettings>? SettingsChanged;

    public void NotifyChanged(AppSettings settings) => SettingsChanged?.Invoke(settings);
}
