using System.Text;
using Business.Utils.Helper;

namespace Business.Data.StorageSpace.Utils;

public static class FileStreamExtension
{
    public static bool CheckPathExists(this string filePath)
    {
        return File.Exists(filePath) || string.IsNullOrEmpty(filePath);
    }

    public static async Task<MemoryStream> ReadStreamWithLimitAsync(this Stream clientStream, int maxBufferSizeInBytes = 4 * 1024 * 1024) // 4MB limit
    {
        const int bufferSize = 8192; // 8 KB buffer size
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        int remainingSize = maxBufferSizeInBytes;

        // Create a MemoryStream to hold the stream data with a capacity of maxBufferSizeInBytes
        MemoryStream memoryStream = new MemoryStream();

        // Read the client stream in chunks and write to the memory stream until the limit is reached
        while ((bytesRead = await clientStream.ReadAsync(buffer, 0, Math.Min(remainingSize, bufferSize))) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead);
            remainingSize -= bytesRead;
        }

        // Optionally, reset the memory stream's position to the beginning if you plan to read from it later
        memoryStream.Position = 0;

        return memoryStream;
    }

    public static string DetectContentType(this byte[] buffer0, byte[] buffer2)
    {
        return buffer0.Concat(buffer2).ToArray().GetCorrectExtension("");
    }

    public static List<Task> CreateWriteTasks(this List<FileStream?> fileStreams, int stripeCount, int[] byteWrites, CancellationToken cancellationToken, byte[][] buffers)
    {
        int stripeIndex = stripeCount % fileStreams.Count;
        int lastIndex = fileStreams.Count - 1;
        int fileStripeIndex = lastIndex - stripeIndex;
        int index = 0;

        List<Task> tasks = [];
        for (int i = 0; i < fileStreams.Count; i++)
        {
            if (fileStripeIndex == i)
            {
                tasks.Add(fileStreams[i]?.WriteAsync(buffers[lastIndex], 0, byteWrites[lastIndex], cancellationToken) ?? Task.CompletedTask);
                continue;
            }

            tasks.Add(fileStreams[i]?.WriteAsync(buffers[index], 0, byteWrites[index], cancellationToken) ?? Task.CompletedTask);
            index++;
        }

        return tasks;
    }

    public static List<Task> CreateWriteTasks(this FileStream?[] fileStreams, int stripeCount, int[] byteWrites, CancellationToken cancellationToken, byte[][] buffers)
    {
        int stripeIndex = stripeCount % fileStreams.Length;
        int lastIndex = fileStreams.Length - 1;
        int fileStripeIndex = lastIndex - stripeIndex;
        int index = 0;

        List<Task> tasks = [];
        for (int i = 0; i < fileStreams.Length; i++)
        {
            if (fileStripeIndex == i)
            {
                tasks.Add(fileStreams[i]?.WriteAsync(buffers[lastIndex], 0, byteWrites[lastIndex], cancellationToken) ?? Task.CompletedTask);
                continue;
            }

            tasks.Add(fileStreams[i]?.WriteAsync(buffers[index], 0, byteWrites[index], cancellationToken) ?? Task.CompletedTask);
            index++;
        }

        return tasks;
    }

    public static string ConvertChecksumToHex(this byte[]? hash)
    {
        StringBuilder checksumBuilder = new();
        if (hash != null)
        {
            foreach (byte b in hash)
            {
                checksumBuilder.Append(b.ToString("x2"));
            }
        }

        return checksumBuilder.ToString();
    }

    public static List<FileStream?> OpenFile(this IEnumerable<string> filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize)
    {
        List<FileStream?> fileStreams = new();
        int i = 0;
        foreach (var path in filePath)
        {
            try
            {
                fileStreams.Add(new FileStream(path, mode, access, share, useAsync: true, bufferSize: bufferSize));
            }
            catch (Exception)
            {
                fileStreams.Add(default);
            }

            i++;
        }

        return fileStreams;
    }

    public static void Seek(this IEnumerable<FileStream?> files, long offset, SeekOrigin origin)
    {
        foreach (var file in files)
        {
            if (file is { CanSeek: true })
                file.Seek(offset, origin);
        }
    }

    private static async Task ReadDiskWithParity(this FileStream?[] fileStreams, byte[][] buffers, int[] byteReads, int parityIndex)
    {
        var length = buffers.Length;
        var readSize = buffers[0].Length;
        Task<int>[] tasks = new Task<int>[length];

        for (int i = 0; i < length; i++)
        {
            var fileStream = fileStreams[i];
            if (fileStream != null)
            {
                tasks[i] = fileStream.ReadAsync(buffers[i], 0, readSize);
            }
        }

        await Task.WhenAll(tasks);
        for (int i = 0; i < length; i++)
        {
            byteReads[i] = await tasks[i];
        }
    }
}