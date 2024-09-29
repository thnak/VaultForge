using System.Numerics;
using System.Security.Cryptography;
using System.Text;

async Task ReadAsync(List<string> source, Stream outputStream, int stripeSize)
{
    int numDisks = source.Count;
    byte[] buffer = new byte[stripeSize];

    // Initialize streams for each disk
    var diskStreams = source.Select(x => new FileStream(
        x,
        FileMode.Open,
        FileAccess.Read,
        FileShare.None,
        stripeSize,
        true)).ToList();

    int stripeIndex = numDisks - 1;
    int currentIndex = 0;
    long totalBytes = 0;
    int totalBytesRead = 0;
    int numberOfEnd = 0;
    while (true)
    {
        totalBytesRead = await diskStreams[currentIndex].ReadAsync(buffer, 0, stripeSize);
        totalBytes += totalBytesRead;
        if (currentIndex != stripeIndex)
            await outputStream.WriteAsync(buffer, 0, totalBytesRead);
        currentIndex++;
        if (currentIndex == numDisks)
            currentIndex = 0;
        stripeIndex--;
        if (stripeIndex < 0)
            stripeIndex = numDisks - 1;
        if (totalBytesRead == 0)
            numberOfEnd++;
        if (numberOfEnd == numDisks) break;
    }

    foreach (var stream in diskStreams)
    {
        await stream.FlushAsync();
        stream.Dispose();
    }

    Console.WriteLine($@"{totalBytes:N0} bytes written restore");
}

async Task WriteDataAsync(Stream inputStream, int stripeSize, string file1Path, string file2Path, string file3Path)
{
    // Open file streams for writing
    await using (FileStream file1 = new FileStream(file1Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize, useAsync: true))
    await using (FileStream file2 = new FileStream(file2Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize, useAsync: true))
    await using (FileStream file3 = new FileStream(file3Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize, useAsync: true))
    {
        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        int bytesRead1, bytesRead2;
        int stripeIndex = 0;

        while ((bytesRead1 = await inputStream.ReadAsync(buffer1, 0, stripeSize)) > 0)
        {
            bytesRead2 = await inputStream.ReadAsync(buffer2, 0, stripeSize);
            var parityBuffer = XorParity(buffer1, buffer2);

            // Determine which file gets the parity for this stripe
            switch (stripeIndex % 3)
            {
                case 0:
                    // Parity goes to file 3
                    await file1.WriteAsync(buffer1, 0, bytesRead1);
                    await file2.WriteAsync(buffer2, 0, bytesRead2);
                    await file3.WriteAsync(parityBuffer, 0, Math.Max(bytesRead1, bytesRead2));
                    break;
                case 1:
                    // Parity goes to file 2
                    await file1.WriteAsync(buffer1, 0, bytesRead1);
                    await file2.WriteAsync(parityBuffer, 0, Math.Max(bytesRead1, bytesRead2));
                    await file3.WriteAsync(buffer2, 0, bytesRead2);
                    break;
                case 2:
                    // Parity goes to file 1
                    await file1.WriteAsync(parityBuffer, 0, Math.Max(bytesRead1, bytesRead2));
                    await file2.WriteAsync(buffer1, 0, bytesRead1);
                    await file3.WriteAsync(buffer2, 0, bytesRead2);
                    break;
            }

            stripeIndex++;
        }
    }
}

async Task ReadDataAsync(Stream outputStream, int stripeSize, string file1Path, string file2Path, string file3Path)
{
    // Open file streams for reading
    using (FileStream file1 = new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true))
    using (FileStream file2 = new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true))
    using (FileStream file3 = new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true))
    {
        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        byte[] parityBuffer = new byte[stripeSize];
        int bytesRead1, bytesRead2;

        int stripeIndex = 0;

        while (true)
        {
            switch (stripeIndex % 3)
            {
                case 0:
                    // Parity in file 3, data in file 1 and file 2
                    bytesRead1 = await file1.ReadAsync(buffer1, 0, stripeSize);
                    bytesRead2 = await file2.ReadAsync(buffer2, 0, stripeSize);
                    if (bytesRead1 == 0 && bytesRead2 == 0) return; // End of stream
                    await outputStream.WriteAsync(buffer1, 0, bytesRead1);
                    await outputStream.WriteAsync(buffer2, 0, bytesRead2);
                    // Read parity, but we don't need to use it here
                    _ = await file3.ReadAsync(parityBuffer, 0, stripeSize);
                    break;

                case 1:
                    // Parity in file 2, data in file 1 and file 3
                    bytesRead1 = await file1.ReadAsync(buffer1, 0, stripeSize);
                    bytesRead2 = await file3.ReadAsync(buffer2, 0, stripeSize);
                    if (bytesRead1 == 0 && bytesRead2 == 0) return; // End of stream
                    await outputStream.WriteAsync(buffer1, 0, bytesRead1);
                    await outputStream.WriteAsync(buffer2, 0, bytesRead2);
                    // Read parity, but we don't need to use it here
                    _ = await file2.ReadAsync(parityBuffer, 0, stripeSize);
                    break;

                case 2:
                    // Parity in file 1, data in file 2 and file 3
                    bytesRead1 = await file2.ReadAsync(buffer1, 0, stripeSize);
                    bytesRead2 = await file3.ReadAsync(buffer2, 0, stripeSize);
                    if (bytesRead1 == 0 && bytesRead2 == 0) return; // End of stream
                    await outputStream.WriteAsync(buffer1, 0, bytesRead1);
                    await outputStream.WriteAsync(buffer2, 0, bytesRead2);
                    // Read parity, but we don't need to use it here
                    _ = await file1.ReadAsync(parityBuffer, 0, stripeSize);
                    break;
            }

            stripeIndex++;
        }
    }
}

async Task ReadDataWithRecoveryAsync(Stream outputStream, int stripeSize, string file1Path, string file2Path, string file3Path)
{
    // Check if any of the files are corrupted or missing
    bool isFile1Corrupted = !File.Exists(file1Path);
    bool isFile2Corrupted = !File.Exists(file2Path);
    bool isFile3Corrupted = !File.Exists(file3Path);

    if (isFile1Corrupted && isFile2Corrupted && isFile3Corrupted)
    {
        throw new Exception("All files are corrupted or missing. Data recovery is impossible.");
    }

    // Open streams for files that exist
    await using (FileStream? file1 = isFile1Corrupted ? null : new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true))
    await using (FileStream? file2 = isFile2Corrupted ? null : new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true))
    await using (FileStream? file3 = isFile3Corrupted ? null : new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true))
    {
        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        byte[] parityBuffer = new byte[stripeSize];
        int bytesRead1, bytesRead2;

        int stripeIndex = 0;

        while (true)
        {
            // Determine the current stripe pattern and read from available files
            switch (stripeIndex % 3)
            {
                case 0:
                    // Parity in file 3, data in file 1 and file 2
                    if (isFile1Corrupted)
                    {
                        // Recover data1 using parity and data2
                        bytesRead1 = await file2!.ReadAsync(buffer2, 0, stripeSize);
                        bytesRead2 = await file3!.ReadAsync(parityBuffer, 0, stripeSize);

                        buffer1 = XorParity(parityBuffer, buffer2);
                    }
                    else if (isFile2Corrupted)
                    {
                        // Recover data2 using parity and data1
                        bytesRead1 = await file1!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file3!.ReadAsync(parityBuffer, 0, stripeSize);
                        buffer2 = XorParity(parityBuffer, buffer1);
                    }
                    else if (isFile3Corrupted)
                    {
                        // Read data, calculate parity to verify correctness
                        bytesRead1 = await file1!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file2!.ReadAsync(buffer2, 0, stripeSize);
                    }
                    else
                    {
                        bytesRead1 = await file1!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file2!.ReadAsync(buffer2, 0, stripeSize);
                        _ = await file3!.ReadAsync(parityBuffer, 0, stripeSize);
                    }

                    if (bytesRead1 == 0 && bytesRead2 == 0) return; // End of stream
                    await outputStream.WriteAsync(buffer1, 0, bytesRead1);
                    await outputStream.WriteAsync(buffer2, 0, bytesRead2);
                    break;

                case 1:
                    // Parity in file 2, data in file 1 and file 3
                    if (isFile1Corrupted)
                    {
                        bytesRead2 = await file3!.ReadAsync(buffer2, 0, stripeSize);
                        bytesRead1 = await file2!.ReadAsync(parityBuffer, 0, stripeSize);
                        buffer1 = XorParity(parityBuffer, buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        bytesRead1 = await file1!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file2!.ReadAsync(parityBuffer, 0, stripeSize);

                        buffer2 = XorParity(parityBuffer, buffer1);
                    }
                    else if (isFile2Corrupted)
                    {
                        bytesRead1 = await file1!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file3!.ReadAsync(buffer2, 0, stripeSize);
                    }
                    else
                    {
                        bytesRead1 = await file1!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file3!.ReadAsync(buffer2, 0, stripeSize);
                        _ = await file2!.ReadAsync(parityBuffer, 0, stripeSize);
                    }

                    if (bytesRead1 == 0 && bytesRead2 == 0) return; // End of stream
                    await outputStream.WriteAsync(buffer1, 0, bytesRead1);
                    await outputStream.WriteAsync(buffer2, 0, bytesRead2);
                    break;

                case 2:
                    // Parity in file 1, data in file 2 and file 3
                    if (isFile2Corrupted)
                    {
                        bytesRead2 = await file3!.ReadAsync(buffer2, 0, stripeSize);
                        bytesRead1 = await file1!.ReadAsync(parityBuffer, 0, stripeSize);
                        buffer1 = XorParity(parityBuffer, buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        bytesRead1 = await file2!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file1!.ReadAsync(parityBuffer, 0, stripeSize);
                        buffer2 = XorParity(parityBuffer, buffer1);
                    }
                    else if (isFile1Corrupted)
                    {
                        bytesRead1 = await file2!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file3!.ReadAsync(buffer2, 0, stripeSize);
                    }
                    else
                    {
                        bytesRead1 = await file2!.ReadAsync(buffer1, 0, stripeSize);
                        bytesRead2 = await file3!.ReadAsync(buffer2, 0, stripeSize);
                        _ = await file1!.ReadAsync(parityBuffer, 0, stripeSize);
                    }

                    if (bytesRead1 == 0 && bytesRead2 == 0) return; // End of stream
                    await outputStream.WriteAsync(buffer1, 0, bytesRead1);
                    await outputStream.WriteAsync(buffer2, 0, bytesRead2);
                    break;
            }

            stripeIndex++;
            await outputStream.FlushAsync();
        }
    }
}


List<string> disks = new List<string>
{
    "C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/Disk1",
    "C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/Disk2",
    "C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/Disk3"
};

disks = disks.Select(x => Path.Combine(x, Path.GetRandomFileName())).ToList();

var fileStream = File.Open("C:/Users/thanh/Downloads/downtime report.xlsx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

using SHA256 sha256 = SHA256.Create();
int readByte = 0;
int totalBytesRead1 = 0;
byte[] buffer1 = new byte[1024];
byte[] buffer2 = new byte[1024];
while ((readByte = await fileStream.ReadAsync(buffer1, 0, 1024)) > 0)
{
    sha256.TransformBlock(buffer1, 0, readByte, null, 0);
    totalBytesRead1 += readByte;
    // if(totalBytesRead == 1024 * 15) break;
}

sha256.TransformFinalBlock([], 0, 0);

StringBuilder checksum = new StringBuilder();
if (sha256.Hash != null)
{
    foreach (byte b in sha256.Hash)
    {
        checksum.Append(b.ToString("x2"));
    }
}

var checkSumStr = checksum.ToString();
sha256.Clear();
fileStream.Seek(0, SeekOrigin.Begin);

var outPutStream = new FileStream("C:/Users/thanh/Downloads/output.xlsx", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024);

int tripSize = 1024;
await WriteDataAsync(fileStream, tripSize, disks[0], disks[1], disks[2]);
await ReadDataWithRecoveryAsync(outPutStream, tripSize, disks[0], disks[1], disks[2]);

outPutStream.Seek(0, SeekOrigin.Begin);
var sha2566 = SHA256.Create();
int totalBytesRead2 = 0;
while ((readByte = await outPutStream.ReadAsync(buffer2, 0, 1024)) > 0)
{
    sha2566.TransformBlock(buffer2, 0, readByte, null, 0);
    totalBytesRead2 += readByte;
    // if(totalBytesRead == 1024 * 15) break;
}

sha2566.TransformFinalBlock([], 0, 0);

checksum = new StringBuilder();
if (sha2566.Hash != null)
{
    foreach (byte b in sha2566.Hash)
    {
        checksum.Append(b.ToString("x2"));
    }
}

var outcheckSumStr = checksum.ToString();
if (checkSumStr != outcheckSumStr)
{
    Console.WriteLine("Checksum Error");
}
else
{
    Console.WriteLine("Checksum OK");
}

sha2566.Clear();
fileStream.Seek(0, SeekOrigin.Begin);

byte[] XorParity(byte[] data0, byte[] data1)
{
    int vectorSize = Vector<byte>.Count;
    int i = 0;

    byte[] parity = new byte[data0.Length];

    // Process in chunks of Vector<byte>.Count (size of SIMD vector)
    if (Vector.IsHardwareAccelerated)
    {
        for (; i <= data1.Length - vectorSize; i += vectorSize)
        {
            // Load the current portion of the parity and data as vectors
            var data0Vector = new Vector<byte>(data0, i);
            var data1Vector = new Vector<byte>(data1, i);

            // XOR the vectors
            var resultVector = Vector.Xor(data0Vector, data1Vector);

            // Store the result back into the parity array
            resultVector.CopyTo(parity, i);
        }

        return parity;
    }

    // Fallback to scalar XOR for the remaining bytes (if any)
    for (; i < data1.Length; i++)
    {
        parity[i] = (byte)(data0[i] ^ data1[i]);
    }

    return parity;
}