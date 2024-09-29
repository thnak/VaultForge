using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using BusinessModels.Utils;

async Task<long> WriteDataAsync(Stream inputStream, int stripeSize, string file1Path, string file2Path, string file3Path, CancellationToken cancellationToken = default)
    {
        long totalByteRead = 0;
        inputStream.SeekBeginOrigin();
        await using FileStream file1 = new FileStream(file1Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize * 10);
        await using FileStream file2 = new FileStream(file2Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize * 10);
        await using FileStream file3 = new FileStream(file3Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize * 10);

        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        int bytesRead1;
        int stripeIndex = 0;

        while ((bytesRead1 = await inputStream.ReadAsync(buffer1, 0, stripeSize, cancellationToken)) > 0)
        {
            var bytesRead2 = await inputStream.ReadAsync(buffer2, 0, stripeSize, cancellationToken);
            totalByteRead += bytesRead1 + bytesRead2;

            var parityBuffer = XorParity(buffer1, buffer2);

            // Create tasks for writing data and parity in parallel
            Task[] writeTasks = [];

            switch (stripeIndex % 3)
            {
                case 0:
                    // Parity goes to file 3
                    writeTasks =
                    [
                        file1.WriteAsync(buffer1, 0, bytesRead1, cancellationToken),
                        file2.WriteAsync(buffer2, 0, bytesRead2, cancellationToken),
                        file3.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken)
                    ];
                    break;

                case 1:
                    // Parity goes to file 2
                    writeTasks =
                    [
                        file1.WriteAsync(buffer1, 0, bytesRead1, cancellationToken),
                        file2.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken),
                        file3.WriteAsync(buffer2, 0, bytesRead2, cancellationToken)
                    ];
                    break;

                case 2:
                    // Parity goes to file 1
                    writeTasks =
                    [
                        file1.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken),
                        file2.WriteAsync(buffer1, 0, bytesRead1, cancellationToken),
                        file3.WriteAsync(buffer2, 0, bytesRead2, cancellationToken)
                    ];
                    break;
            }

            // Wait for all tasks (writes) to complete in parallel
            await Task.WhenAll(writeTasks);

            stripeIndex++;
        }

        await file1.FlushAsync(cancellationToken);
        await file2.FlushAsync(cancellationToken);
        await file3.FlushAsync(cancellationToken);
        return totalByteRead;
    }

async Task ReadDataWithRecoveryAsync(Stream outputStream, int stripeSize, long originalFileSize, string file1Path, string file2Path, string file3Path, long seekPosition = 0)
{
    // Check if any of the files are corrupted or missing
    bool isFile1Corrupted = !File.Exists(file1Path) || string.IsNullOrEmpty(file1Path);
    bool isFile2Corrupted = !File.Exists(file2Path) || string.IsNullOrEmpty(file2Path);
    bool isFile3Corrupted = !File.Exists(file3Path) || string.IsNullOrEmpty(file3Path);
    
    
    long totalBytesWritten = seekPosition;
    if (isFile1Corrupted && isFile2Corrupted || isFile3Corrupted && isFile1Corrupted || isFile2Corrupted && isFile3Corrupted)
    {
        throw new Exception("More than 2 disk are failure. Data recovery is impossible.");
    }

    // Open streams for files that exist
    await using FileStream? file1 = isFile1Corrupted ? null : new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize);
    await using FileStream? file2 = isFile2Corrupted ? null : new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize);
    await using FileStream? file3 = isFile3Corrupted ? null : new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize);

    byte[] buffer1 = new byte[stripeSize];
    byte[] buffer2 = new byte[stripeSize];
    byte[] parityBuffer = new byte[stripeSize];
    
    int stripeIndex = 0;

    while (totalBytesWritten < originalFileSize)
    {
        Task<int> readTask1 = Task.FromResult(0);
        Task<int> readTask2 = Task.FromResult(0);
        Task<int> readTask3;

        // Determine the current stripe pattern and read from available files
        switch (stripeIndex % 3)
        {
            case 0:
                // Parity in file 3, data in file 1 and file 2
                if (isFile1Corrupted)
                {
                    // Recover data1 using parity and data2
                    readTask2 = file2!.ReadAsync(buffer2, 0, stripeSize);
                    readTask1 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                    buffer1 = XorParity(parityBuffer, buffer2);
                }
                else if (isFile2Corrupted)
                {
                    // Recover data2 using parity and data1
                    readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                    buffer2 = XorParity(parityBuffer, buffer1);
                }
                else if (isFile3Corrupted)
                {
                    // Read data, calculate parity to verify correctness
                    readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file2!.ReadAsync(buffer2, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                }
                else
                {
                    readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file2!.ReadAsync(buffer2, 0, stripeSize);
                    readTask3 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2, readTask3);
                }

                break;

            case 1:
                // Parity in file 2, data in file 1 and file 3
                if (isFile1Corrupted)
                {
                    readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                    readTask1 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                    buffer1 = XorParity(parityBuffer, buffer2);
                }
                else if (isFile3Corrupted)
                {
                    readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                    buffer2 = XorParity(parityBuffer, buffer1);
                }
                else if (isFile2Corrupted)
                {
                    readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                }
                else
                {
                    readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                    readTask3 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2, readTask3);
                }

                break;

            case 2:
                // Parity in file 1, data in file 2 and file 3
                if (isFile2Corrupted)
                {
                    readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                    readTask1 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                    buffer1 = XorParity(parityBuffer, buffer2);
                }
                else if (isFile3Corrupted)
                {
                    readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                    buffer2 = XorParity(parityBuffer, buffer1);
                }
                else if (isFile1Corrupted)
                {
                    readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2);
                }
                else
                {
                    readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                    readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                    readTask3 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                    await Task.WhenAll(readTask1, readTask2, readTask3);
                }

                break;
        }

        var bytesRead1 = await readTask1;
        var bytesRead2 = await readTask2;

        if (bytesRead1 == 0 && bytesRead2 == 0)
        {
            break; // End of stream
        }

        var writeSize1 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead1);
        await outputStream.WriteAsync(buffer1, 0, writeSize1);
        totalBytesWritten += writeSize1;

        var writeSize2 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead2);
        await outputStream.WriteAsync(buffer2, 0, writeSize2);
        totalBytesWritten += writeSize2;

        stripeIndex++;
    }

    await outputStream.FlushAsync();
}


List<string> disks = new List<string>
{
    "C:/Users/thanh/Git/CodeWithMe/ResApi/bin/Debug/net9.0/disk1\\24-09-29\\66f95d8da244b00d7c603ec5lzbj4ehh.vcb",
    "C:/Users/thanh/Git/CodeWithMe/ResApi/bin/Debug/net9.0/disk2\\24-09-29\\66f95d8da244b00d7c603ec5gdydmxz1.ez1",
    "C:/Users/thanh/Git/CodeWithMe/ResApi/bin/Debug/net9.0/disk3\\24-09-29\\66f95d8da244b00d7c603ec5gb2trfqu.xns"
};

// disks = disks.Select(x => Path.Combine(x, Path.GetRandomFileName())).ToList();

var fileStream = File.Open("C:/Users/thanh/Downloads/469.tif", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

using SHA256 sha256 = SHA256.Create();
int readByte = 0;
int totalBytesRead1 = 0;
byte[] buffer1 = new byte[1024];
byte[] buffer2 = new byte[1024];


// var outPutStream = new FileStream("C:/Users/thanh/Downloads/output.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024);
var outPutStream = new MemoryStream();
int tripSize = 1024;
// var totalByteRead = await WriteDataAsync(fileStream, tripSize, disks[0], disks[1], disks[2]);
// Console.WriteLine($"Total bytes read: {totalByteRead:N0}");
await ReadDataWithRecoveryAsync(outPutStream, tripSize, 24772046, disks[0], disks[1], disks[2]);
fileStream.Seek(0, SeekOrigin.Begin);
while ((readByte = await fileStream.ReadAsync(buffer1, 0, 1024)) > 0)
{
    sha256.TransformBlock(buffer1, 0, readByte, null, 0);
    totalBytesRead1 += readByte;
    // if (totalBytesRead1 >= totalByteRead - 1024) break;
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
outPutStream.Seek(0, SeekOrigin.Begin);
var sha2566 = SHA256.Create();
int totalBytesRead2 = 0;
while ((readByte = await outPutStream.ReadAsync(buffer2, 0, 1024)) > 0)
{
    sha2566.TransformBlock(buffer2, 0, readByte, null, 0);
    totalBytesRead2 += readByte;
    // if (totalBytesRead2 >= totalByteRead - 1024)
    //     break;
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

for (int i = 0; i < buffer1.Length; i++)
{
    if (buffer1[i] != buffer2[i])
    {
        Console.WriteLine($"Buffer Error {i}");
        break;
    }
}

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