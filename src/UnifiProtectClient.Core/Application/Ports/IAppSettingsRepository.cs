using System.Threading;
using System.Threading.Tasks;
using UnifiProtectClient.Application.Settings;

namespace UnifiProtectClient.Application.Ports;

public interface IAppSettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
