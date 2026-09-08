using System;
using Surveil.Application.Ports;
using Surveil.Application.Settings;

namespace Surveil.Infrastructure.Settings;

public sealed class SettingsChangeNotifier : ISettingsChangeNotifier
{
    public event Action<AppSettings>? SettingsChanged;

    public void NotifyChanged(AppSettings settings) => SettingsChanged?.Invoke(settings);
}
