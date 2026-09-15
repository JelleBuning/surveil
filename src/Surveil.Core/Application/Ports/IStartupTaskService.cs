using System.Threading;
using System.Threading.Tasks;

namespace Surveil.Application.Ports;

public interface IStartupTaskService
{
    Task<StartupTaskStatus> GetStatusAsync(CancellationToken ct = default);
    Task<StartupTaskStatus> EnableAsync(CancellationToken ct = default);
    Task<StartupTaskStatus> DisableAsync(CancellationToken ct = default);
}
