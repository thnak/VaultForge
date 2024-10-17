using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Business.Services.Ffmpeg;

public class FFmpegService
{
    public static void EncodeVideo(string inputFilePath, string outputDir)
    {
        // Detect resolution of input file
        string resolution = GetVideoResolution(inputFilePath);

        // Dynamically adjust output resolutions
        string[] resolutions = GetOutputResolutions(resolution);

        // Build the FFmpeg command
        string ffmpegCommand = BuildFFmpegCommand(inputFilePath, resolutions, outputDir);

        // Execute the FFmpeg command
        ExecuteFFmpegCommand(ffmpegCommand);

        Console.WriteLine("Success!");
    }

    private static string GetVideoResolution(string inputFile)
    {
        // Run FFmpeg to get resolution
        string ffmpegProbeCommand = $"ffprobe -v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{inputFile}\"";
        string resolution = ExecuteCommand(ffmpegProbeCommand);

        return resolution;
    }

    private static string[] GetOutputResolutions(string inputResolution)
    {
        if (inputResolution == "3840x2160")
            return new string[] { "3840x2160", "2560x1440", "1920x1080", "1280x720", "854x480" };
        else if (inputResolution == "2560x1440")
            return new string[] { "2560x1440", "1920x1080", "1280x720", "854x480" };
        else if (inputResolution == "1920x1080")
            return new string[] { "1920x1080", "1280x720", "854x480" };
        else
            return new string[] { "1280x720", "854x480" };
    }

    private static string BuildFFmpegCommand(string inputFile, string[] resolutions, string outputDir)
    {
        var videoStreamCount = GetVideoStreamCount(inputFile);
        
        // Detect hardware acceleration
        var hardWare = HardwareDetectionService.DetectHardwareAcceleration();

        // Start constructing the FFmpeg command
        string ffmpegCommand = $"ffmpeg -hwaccel {hardWare} -i \"{inputFile}\" ";

        // Add filters and scaling commands dynamically
        ffmpegCommand += "-filter_complex \"[0:v]split=" + resolutions.Length;
        for (int i = 0; i < resolutions.Length; i++)
        {
            ffmpegCommand += $"[v{i}]";
        }

        ffmpegCommand += "; ";

        // Add the scaling for each resolution
        for (int i = 0; i < resolutions.Length; i++)
        {
            ffmpegCommand += $"[v{i}]scale={resolutions[i]}[v{i}out]; ";
        }

        // Determine codec based on hardware acceleration
        var codec = "av1";
        if (hardWare == "cuda")
            codec = "av1_nvenc";
        else if (hardWare == "qsv")
            codec = "av1_qsv";
        else if (hardWare == "vaapi")
            codec = "av1_vaapi";

        // Prepare output mappings
        ffmpegCommand += "\" ";

        // Add mapping for each resolution
        for (int i = 0; i < resolutions.Length; i++)
        {
            ffmpegCommand += $"-map \"[v{i}out]\" -c:v:{i} {codec} -b:v:{i} 8000k -maxrate:v:{i} 9000k -bufsize:v:{i} 12000k ";
            ffmpegCommand += "-g 48 -keyint_min 48 -pix_fmt p010le -colorspace bt2020nc -color_trc smpte2084 -color_primaries bt2020 ";
        }

        // Add HLS options
        ffmpegCommand += $"-f hls -hls_time 6 -hls_list_size 0 -master_pl_name \"master.m3u8\" ";
        ffmpegCommand += $"-hls_segment_filename \"" + Path.Combine(outputDir, "stream_%v_%03d.ts") + "\" ";
        ffmpegCommand += "-var_stream_map \"";

        // Add variable stream mapping
        for (int i = 0; i < resolutions.Length; i++)
        {
            ffmpegCommand += $"v:{i},a:{i} ";
        }

        ffmpegCommand = ffmpegCommand.TrimEnd(); // Remove the trailing space
        ffmpegCommand += $"\" \"" + Path.Combine(outputDir, "stream_%v.m3u8") + "\"";

        return ffmpegCommand;
    }


    private static int GetVideoStreamCount(string inputFile)
    {
        var output = ExecuteCommand($"ffmpeg -hide_banner -i \"{inputFile}\"");

        // Count video streams from the output
        // This is a simple check, customize this as per your needs
        int count = output.Split(new[] { "Stream #" }, StringSplitOptions.RemoveEmptyEntries)
            .Count(x => x.Contains("Video:"));

        return count;
    }

    private static void ExecuteFFmpegCommand(string command)
    {
        ExecuteCommand(command);
    }

    private static string ExecuteCommand(string command)
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
            using var reader = process.StandardOutput;
            string output = reader.ReadToEnd().Trim();
            Console.WriteLine(output);
            return output;
        }

        return string.Empty;
    }
}