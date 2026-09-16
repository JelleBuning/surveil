using System;
using Surveil.Application.Settings;

namespace Surveil.Application.Ports;

public interface ISettingsChangeNotifier
{
    event Action<AppSettings> SettingsChanged;

    void NotifyChanged(AppSettings settings);
}
