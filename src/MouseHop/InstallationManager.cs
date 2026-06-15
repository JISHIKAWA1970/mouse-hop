using System.Diagnostics;

namespace MouseHop;

internal static class InstallationManager
{
    private const string ExecutableName = "MouseHop.exe";

    internal static string StandardDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "MouseHop");

    internal static string StandardExecutablePath => Path.Combine(StandardDirectory, ExecutableName);

    internal static InstallationStatus GetStatus()
    {
        var currentPath = Environment.ProcessPath ?? string.Empty;
        var canInstall = IsSelfContainedExecutable(currentPath);
        var isInstalled = IsStandardPath(currentPath);

        return new InstallationStatus(
            currentPath,
            StandardExecutablePath,
            isInstalled,
            canInstall);
    }

    internal static InstallationResult InstallToStandardLocationAndRestart()
    {
        var status = GetStatus();
        if (status.IsInstalled)
        {
            return InstallationResult.Success(shouldExitCurrentProcess: false);
        }

        if (!status.CanInstall)
        {
            return InstallationResult.Failure("dotnet run や DLL 経由の実行中は標準フォルダへ配置できません。publish 済みの MouseHop.exe から起動して実行してください。");
        }

        try
        {
            Directory.CreateDirectory(StandardDirectory);
            File.Copy(status.CurrentExecutablePath, status.StandardExecutablePath, overwrite: true);
        }
        catch (Exception exception)
        {
            return InstallationResult.Failure($"標準フォルダへのコピーに失敗しました: {exception.Message}");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = status.StandardExecutablePath,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            return InstallationResult.Failure($"標準フォルダの MouseHop.exe を起動できませんでした: {exception.Message}");
        }

        return InstallationResult.Success(shouldExitCurrentProcess: true);
    }

    internal static bool IsStandardPath(string executablePath)
    {
        return !string.IsNullOrWhiteSpace(executablePath)
            && string.Equals(
                Path.GetFullPath(executablePath),
                Path.GetFullPath(StandardExecutablePath),
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSelfContainedExecutable(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath)
            || !string.Equals(Path.GetFileName(executablePath), ExecutableName, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(executablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directoryParts = baseDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !directoryParts.Any(part => string.Equals(part, "bin", StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed record InstallationStatus(
    string CurrentExecutablePath,
    string StandardExecutablePath,
    bool IsInstalled,
    bool CanInstall);

internal sealed record InstallationResult(bool Succeeded, bool ShouldExitCurrentProcess, string? ErrorMessage)
{
    internal static InstallationResult Success(bool shouldExitCurrentProcess) => new(true, shouldExitCurrentProcess, null);

    internal static InstallationResult Failure(string errorMessage) => new(false, false, errorMessage);
}
