using System;
using System.Linq;

namespace fundo.core;

internal static class CommandLineService
{
    public static bool IsUpdateIndexOnlyLaunch(string launchArguments)
    {
        return HasArgument(launchArguments, "--UpdateIndexOnly");
    }

    private static bool HasArgument(string launchArguments, string argument)
    {
        if (!string.IsNullOrWhiteSpace(launchArguments))
        {
            string[] arguments = launchArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (arguments.Contains(argument, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return Environment.GetCommandLineArgs()
            .Contains(argument, StringComparer.OrdinalIgnoreCase);
    }
}
