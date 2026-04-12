using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace fundo.tool;

internal static class AutostartService
{
    private const string StartupTaskId = "FundoAutostart";

    /// <summary>
    /// Enables or disables the Windows startup task to match the desired setting.
    /// </summary>
    public static async Task ApplyAutostartSettingAsync(bool enabled)
    {
        try
        {
            StartupTask startupTask = await StartupTask.GetAsync(StartupTaskId);

            if (enabled)
            {
                if (startupTask.State == StartupTaskState.Disabled)
                {
                    await startupTask.RequestEnableAsync();
                }
            }
            else
            {
                if (startupTask.State == StartupTaskState.Enabled)
                {
                    startupTask.Disable();
                }
            }
        }
        catch
        {
            // Ignore errors (e.g. startup task not declared in manifest)
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the current launch was triggered by the Windows startup task.
    /// </summary>
    public static bool IsAutostartLaunch()
    {
        try
        {
            var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            return activatedArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.StartupTask;
        }
        catch
        {
            return false;
        }
    }
}
