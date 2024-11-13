namespace Business.Data.StorageSpace.Utils;

public static class FileStreamExtension
{
    public static bool CheckPathExists(this string filePath)
    {
        return File.Exists(filePath) || string.IsNullOrEmpty(filePath);
    }

    public static FileStream?[] OpenFile(this List<string> filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize)
    {
        FileStream?[] fileStreams = new FileStream[filePath.Count()];
        for (int i = 0; i < filePath.Count; i++)
        {
            try
            {
                fileStreams[i] = new FileStream(filePath[i], mode, access, share, useAsync: true, bufferSize: bufferSize);
            }
            catch (Exception)
            {
                fileStreams[i] = default;
            }
        }

        return fileStreams;
    }

    public static void Seek(this FileStream?[] files, long offset, SeekOrigin origin)
    {
        foreach (var file in files)
        {
            if(file is { CanSeek: true })
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