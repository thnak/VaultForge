using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Business.Services.Ffmpeg;

public static class HardwareDetectionService
{
    public static string DetectHardwareAcceleration()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (IsNvidiaGpuAvailable())
            {
                return "cuda"; // NVIDIA GPU available, use CUDA
            }

            if (IsIntelQuickSyncAvailable())
            {
                return "qsv"; // Intel QuickSync available
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (IsNvidiaGpuAvailable())
            {
                return "cuda"; // NVIDIA GPU available, use CUDA
            }

            if (IsVaapiAvailable())
            {
                return "vaapi"; // VA-API available, which can be AMD or Intel
            }
        }

        return "none"; // No hardware acceleration detected
    }

    // Check for NVIDIA CUDA support
    private static bool IsNvidiaGpuAvailable()
    {
        try
        {
            var processInfo = new ProcessStartInfo("nvidia-smi")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                return !string.IsNullOrEmpty(output) && output.Contains("NVIDIA");
            }
        }
        catch (Exception)
        {
            // nvidia-smi command not found or no NVIDIA GPU present
            return false;
        }
    }

    // Check for Intel QuickSync (via Windows detection or vainfo on Linux)
    private static bool IsIntelQuickSyncAvailable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, Intel QuickSync detection would be tied to Intel Graphics presence.
            return DetectIntelGraphicsWindows();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return IsVaapiAvailable(); // VA-API for Intel can be checked using vainfo
        }

        return false;
    }

    // Check for VA-API support (for AMD or Intel)
    private static bool IsVaapiAvailable()
    {
        try
        {
            var processInfo = new ProcessStartInfo("vainfo")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                return !string.IsNullOrEmpty(output) && output.Contains("VA-API");
            }
        }
        catch (Exception)
        {
            // vainfo command not found or no VA-API support
            return false;
        }
    }

    // Detect Intel Graphics on Windows
    private static bool DetectIntelGraphicsWindows()
    {
        try
        {
            var processInfo = new ProcessStartInfo("wmic", "path win32_VideoController get name")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                return output.Contains("Intel");
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
}