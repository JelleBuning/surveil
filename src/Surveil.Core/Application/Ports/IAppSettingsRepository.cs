using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Settings;

namespace Surveil.Application.Ports;

public interface IAppSettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
