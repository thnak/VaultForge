using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Business.Data.StorageSpace.Utils;

public static class FileStreamExtension
{
    public static bool CheckPathExists(this string filePath)
    {
        return File.Exists(filePath) || string.IsNullOrEmpty(filePath);
    }

    public static async Task ReadStreamWithLimitAsync(this Stream clientSourceStream, MemoryStream bufferStream, byte[] buffer) // 4MB limit
    {
        int bytesRead;
        bufferStream.Seek(0, SeekOrigin.Begin);
        bufferStream.SetLength(0);
        int remainingSize = bufferStream.Capacity;

        // Read the client stream in chunks and write to the memory stream until the limit is reached
        while ((bytesRead = await clientSourceStream.ReadAsync(buffer, 0, Math.Min(remainingSize, buffer.Length))) > 0)
        {
            await bufferStream.WriteAsync(buffer, 0, bytesRead);
            remainingSize -= bytesRead;
        }

        // Optionally, reset the memory stream's position to the beginning if you plan to read from it later
        bufferStream.Seek(0, SeekOrigin.Begin);
    }


    public static int[] GenerateRaid5Indices(this int fileStreamsCount, int stripeCount)
    {
        int[] indices = new int[fileStreamsCount];

        // Populate indices array based on the stripe row index and disk count
        for (int i = 0; i < fileStreamsCount; i++)
        {
            indices[i] = (i + stripeCount) % fileStreamsCount;
        }

        return indices;
    }

    public static void GenerateRaid5Indices(this int fileStreamsCount, int stripeCount, int[] indices)
    {
        // Populate indices array based on the stripe row index and disk count
        for (int i = 0; i < fileStreamsCount; i++)
        {
            indices[i] = (i + stripeCount) % fileStreamsCount;
        }
    }


    public static List<Task> CreateWriteTasks(this List<FileStream?> fileStreams, int[] indices, int[] byteWrites, CancellationToken cancellationToken, byte[][] buffers)
    {
        List<Task> tasks = [];
        for (int i = 0; i < fileStreams.Count; i++)
        {
            var stream = fileStreams[indices[i]];
            if (stream != null)
            {
                tasks.Add(stream.WriteAsync(buffers[i], 0, byteWrites[i], cancellationToken));
            }
        }

        return tasks;
    }

    public static async Task WriteTasks<T>(this List<T?> fileStreams, int[] indices, int byteWrites, CancellationToken cancellationToken, byte[][] buffers) where T : Stream
    {
        for (int i = 0; i < fileStreams.Count; i++)
        {
            var stream = fileStreams[i];
            if (stream != null)
            {
                await stream.WriteAsync(buffers[indices[i]], 0, byteWrites, cancellationToken);
            }
        }
    }

    public static List<Task> CreateWriteTasks<T>(this List<T?> fileStreams, int[] indices, int byteWrites, CancellationToken cancellationToken, byte[][] buffers) where T : Stream
    {
        List<Task> tasks = [];
        for (int i = 0; i < fileStreams.Count; i++)
        {
            var stream = fileStreams[i];
            if (stream != null)
            {
                tasks.Add(stream.WriteAsync(buffers[indices[i]], 0, byteWrites, cancellationToken));
            }
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

        }

        return fileStreams;
    }

    public static void Seek<T>(this IEnumerable<T?> files, long offset, SeekOrigin origin) where T : Stream
    {
        foreach (var file in files)
        {
            if (file is { CanSeek: true })
                file.Seek(offset, origin);
        }
    }



    public static void XorParity(this byte[] data0, byte[] data1, byte[] parity)
    {
        int vectorSize = Vector<byte>.Count;
        int i = 0;


        // Process in chunks of Vector<byte>.Count (size of SIMD vector)
        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= data1.Length - vectorSize; i += vectorSize)
            {
                // Load the current portion of the parity and data as vectors
                var data0Vector = new Vector<byte>(data0, i);
                var data1Vector = new Vector<byte>(data1, i);

                // XOR the vectors
                var resultVector = data0Vector ^ data1Vector;

                // Store the result back into the parity array
                resultVector.CopyTo(parity, i);
            }
        }

        // Fallback to scalar XOR for the remaining bytes (if any)
        for (; i < data1.Length; i++)
        {
            parity[i] = (byte)(data0[i] ^ data1[i]);
        }
    }

    public static void XorParity(this byte[][] data, byte[] parity)
    {
        // Initialize the result array for storing the XOR parity
        data.First().CopyTo(parity, 0);
        for (int i = 1; i < data.Length; i++)
        {
            parity.XorParity(data[i], parity);
        }
    }

    public static bool CompareHashes(this Stream stream1, Stream stream2)
    {
        // Reset stream positions to ensure we hash the entire content
        stream1.Position = 0;
        stream2.Position = 0;

        using var sha256 = SHA256.Create();
        // Compute hashes
        byte[] hash1 = sha256.ComputeHash(stream1);
        byte[] hash2 = sha256.ComputeHash(stream2);

        // Compare hashes
        return hash1.SequenceEqual(hash2);
    }

    public static void Fill<T>(this T[] array, T value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
    }
}