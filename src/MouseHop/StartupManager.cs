using Microsoft.Win32;

namespace MouseHop;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MouseHop";

    internal static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var registeredValue = key?.GetValue(ValueName) as string;
            return PointsToStandardExecutable(registeredValue);
        }
        catch
        {
            return false;
        }
    }

    internal static StartupChangeResult SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return StartupChangeResult.Failure("Windows の自動起動設定を開けませんでした。");
            }

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return StartupChangeResult.Success();
            }

            var standardExecutablePath = InstallationManager.StandardExecutablePath;
            if (!File.Exists(standardExecutablePath))
            {
                return StartupChangeResult.Failure("自動起動を有効にする前に、先に標準フォルダへ配置してください。");
            }

            key.SetValue(ValueName, Quote(standardExecutablePath), RegistryValueKind.String);
            return StartupChangeResult.Success();
        }
        catch (Exception exception)
        {
            return StartupChangeResult.Failure($"Windows の自動起動設定を変更できませんでした: {exception.Message}");
        }
    }

    private static bool PointsToStandardExecutable(string? registeredValue)
    {
        if (string.IsNullOrWhiteSpace(registeredValue))
        {
            return false;
        }

        var registeredPath = registeredValue.Trim().Trim('"');
        return InstallationManager.IsStandardPath(registeredPath);
    }

    private static string Quote(string path) => $"\"{path}\"";
}

internal sealed record StartupChangeResult(bool Succeeded, string? ErrorMessage)
{
    internal static StartupChangeResult Success() => new(true, null);

    internal static StartupChangeResult Failure(string errorMessage) => new(false, errorMessage);
}
