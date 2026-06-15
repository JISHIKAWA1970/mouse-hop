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
            return !string.IsNullOrWhiteSpace(key?.GetValue(ValueName) as string);
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

            if (!TryGetStartupExecutablePath(out var executablePath, out var errorMessage))
            {
                return StartupChangeResult.Failure(errorMessage);
            }

            key.SetValue(ValueName, Quote(executablePath), RegistryValueKind.String);
            return StartupChangeResult.Success();
        }
        catch (Exception exception)
        {
            return StartupChangeResult.Failure($"Windows の自動起動設定を変更できませんでした: {exception.Message}");
        }
    }

    private static bool TryGetStartupExecutablePath(out string executablePath, out string errorMessage)
    {
        executablePath = Environment.ProcessPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            errorMessage = "現在の実行ファイルパスを取得できないため、自動起動に登録できません。";
            return false;
        }

        if (!string.Equals(Path.GetExtension(executablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "dotnet run や DLL 経由の実行中は自動起動に登録できません。publish 済みの MouseHop.exe から起動して設定してください。";
            return false;
        }

        if (!string.Equals(Path.GetFileName(executablePath), "MouseHop.exe", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "MouseHop.exe 以外の実行ファイルは自動起動に登録できません。publish 済みの MouseHop.exe から起動して設定してください。";
            return false;
        }

        var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directoryParts = baseDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (directoryParts.Any(part => string.Equals(part, "bin", StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "開発用の bin フォルダから実行中は自動起動に登録しません。publish 済みの MouseHop.exe から起動して設定してください。";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static string Quote(string path) => $"\"{path}\"";
}

internal sealed record StartupChangeResult(bool Succeeded, string? ErrorMessage)
{
    internal static StartupChangeResult Success() => new(true, null);

    internal static StartupChangeResult Failure(string errorMessage) => new(false, errorMessage);
}
