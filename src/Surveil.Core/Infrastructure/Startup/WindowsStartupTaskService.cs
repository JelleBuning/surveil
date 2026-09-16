using System;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Windows.ApplicationModel;

namespace Surveil.Infrastructure.Startup;

public sealed class WindowsStartupTaskService : IStartupTaskService
{
    internal const string StartupTaskId = "SurveilStartupTask";

    public async Task<StartupTaskStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var task = await TryGetTaskAsync(ct);
        return task is null ? StartupTaskStatus.Unavailable : Map(task.State);
    }

    public async Task<StartupTaskStatus> EnableAsync(CancellationToken ct = default)
    {
        var task = await TryGetTaskAsync(ct);
        if (task is null)
            return StartupTaskStatus.Unavailable;

        try
        {
            return Map(await task.RequestEnableAsync().AsTask(ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Map(task.State);
        }
    }

    public async Task<StartupTaskStatus> DisableAsync(CancellationToken ct = default)
    {
        var task = await TryGetTaskAsync(ct);
        if (task is null)
            return StartupTaskStatus.Unavailable;

        try
        {
            task.Disable();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }

        return Map(task.State);
    }

    private static async Task<StartupTask?> TryGetTaskAsync(CancellationToken ct)
    {
        try
        {
            return await StartupTask.GetAsync(StartupTaskId).AsTask(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    internal static StartupTaskStatus Map(StartupTaskState state) => state switch
    {
        StartupTaskState.Enabled => StartupTaskStatus.Enabled,
        StartupTaskState.EnabledByPolicy => StartupTaskStatus.EnabledByPolicy,
        StartupTaskState.Disabled => StartupTaskStatus.Disabled,
        StartupTaskState.DisabledByUser => StartupTaskStatus.DisabledByUser,
        StartupTaskState.DisabledByPolicy => StartupTaskStatus.DisabledByPolicy,
        _ => StartupTaskStatus.Unavailable
    };
}
