using System;
using Surveil.Application.Settings;

namespace Surveil.Application.Ports;

/// <summary>Notifies interested services when persisted app settings have been saved.</summary>
public interface ISettingsChangeNotifier
{
    event Action<AppSettings> SettingsChanged;

    void NotifyChanged(AppSettings settings);
}
