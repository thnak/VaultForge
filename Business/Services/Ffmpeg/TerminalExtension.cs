using System.Diagnostics;
using System.Runtime.InteropServices;


namespace Business.Services.Ffmpeg;

public class TerminalExtension
{
    public static async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        ProcessStartInfo processInfo;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows uses cmd.exe
            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Linux and macOS use /bin/bash
            processInfo = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"");
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system");
        }

        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;
        processInfo.UseShellExecute = false;
        processInfo.CreateNoWindow = true;

        using var process = Process.Start(processInfo);
        if (process != null)
        {
            using var outputReader = process.StandardOutput;
            using var errorReader = process.StandardError;

            var error = await errorReader.ReadToEndAsync(cancellationToken);
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($@"[ERROR] {error}");
            }

            string output = (await outputReader.ReadToEndAsync(cancellationToken)).Trim();
            Console.WriteLine(output);
            return output;
        }

        return string.Empty;
    }
}