using System.Numerics;
using System.Security.Cryptography;
using System.Text;

async Task<long> WriteDataAsync(Stream inputStream, int stripeSize, string file1Path, string file2Path, string file3Path)
{
    long totalByteRead = 0;

    await using FileStream file1 = new FileStream(file1Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize, useAsync: true);
    await using FileStream file2 = new FileStream(file2Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize, useAsync: true);
    await using FileStream file3 = new FileStream(file3Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: stripeSize, useAsync: true);

    byte[] buffer1 = new byte[stripeSize];
    byte[] buffer2 = new byte[stripeSize];
    int bytesRead1;
    int stripeIndex = 0;

    while ((bytesRead1 = await inputStream.ReadAsync(buffer1, 0, stripeSize)) > 0)
    {
        var bytesRead2 = await inputStream.ReadAsync(buffer2, 0, stripeSize);
        totalByteRead += bytesRead1 + bytesRead2;

        var parityBuffer = XorParity(buffer1, buffer2);

        // Determine which file gets the parity for this stripe
        switch (stripeIndex % 3)
        {
            case 0:
                // Parity goes to file 3
                await file1.WriteAsync(buffer1, 0, bytesRead1);
                await file2.WriteAsync(buffer2, 0, bytesRead2);
                await file3.WriteAsync(parityBuffer, 0, stripeSize);
                break;
            case 1:
                // Parity goes to file 2\
                await file1.WriteAsync(buffer1, 0, bytesRead1);
                await file2.WriteAsync(parityBuffer, 0, stripeSize);
                await file3.WriteAsync(buffer2, 0, bytesRead2);
                break;
            case 2:
                // Parity goes to file 1
                await file1.WriteAsync(parityBuffer, 0, stripeSize);
                await file2.WriteAsync(buffer1, 0, bytesRead1);
                await file3.WriteAsync(buffer2, 0, bytesRead2);
                break;
        }

        stripeIndex++;
    }


    return totalByteRead;
}
async Task ReadDataWithRecoveryAsync(Stream outputStream, int stripeSize, long originalFileSize, string file1Path, string file2Path, string file3Path)
{
    // Check if any of the files are corrupted or missing
    bool isFile1Corrupted = !File.Exists(file1Path);
    bool isFile2Corrupted = !File.Exists(file2Path);
    bool isFile3Corrupted = !File.Exists(file3Path);
    long totalBytesWritten = 0;
    if (isFile1Corrupted && isFile2Corrupted || isFile3Corrupted && isFile1Corrupted || isFile2Corrupted && isFile3Corrupted)
    {
        throw new Exception("More than 2 disk are failure. Data recovery is impossible.");
    }

    // Open streams for files that exist
    await using FileStream? file1 = isFile1Corrupted ? null : new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true);
    await using FileStream? file2 = isFile2Corrupted ? null : new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true);
    await using FileStream? file3 = isFile3Corrupted ? null : new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true);

    byte[] buffer1 = new byte[stripeSize];
    byte[] buffer2 = new byte[stripeSize];
    byte[] parityBuffer = new byte[stripeSize];

    int stripeIndex = 0;
    int bytesRead1 = 0;
    int bytesRead2 = 0;
    int writeSize1 = 0;
    int writeSize2 = 0;

    while (totalBytesWritten < originalFileSize)
    {
        // Determine the current stripe pattern and read from available files
        switch (stripeIndex % 3)
        {
            case 0:
                // Parity in file 3, data in file 1 and file 2
                if (isFile1Corrupted)
                {
                    // Recover data1 using parity and data2
                    bytesRead2 = await file2!.ReadAsync(buffer2, 0, stripeSize);
                    bytesRead1 = await file3!.ReadAsync(parityBuffer, 0, stripeSize);

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

                if (bytesRead1 == 0 && bytesRead2 == 0)
                {
                    await outputStream.FlushAsync();
                    return; // End of stream
                }

                writeSize1 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead1);
                await outputStream.WriteAsync(buffer1, 0, writeSize1);
                totalBytesWritten += writeSize1;

                writeSize2 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead2);
                await outputStream.WriteAsync(buffer2, 0, writeSize2);
                totalBytesWritten += writeSize2;
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

                if (bytesRead1 == 0 && bytesRead2 == 0)
                {
                    await outputStream.FlushAsync();
                    return; // End of stream
                }


                writeSize1 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead1);
                await outputStream.WriteAsync(buffer1, 0, writeSize1);
                totalBytesWritten += writeSize1;

                writeSize2 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead2);
                await outputStream.WriteAsync(buffer2, 0, writeSize2);
                totalBytesWritten += writeSize2;
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

                if (bytesRead1 == 0 && bytesRead2 == 0)
                {
                    await outputStream.FlushAsync();
                    return; // End of stream
                }

                writeSize1 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead1);
                await outputStream.WriteAsync(buffer1, 0, writeSize1);
                totalBytesWritten += writeSize1;

                writeSize2 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead2);
                await outputStream.WriteAsync(buffer2, 0, writeSize2);
                totalBytesWritten += writeSize2;
                break;
        }

        await outputStream.FlushAsync();
        stripeIndex++;
    }
}


List<string> disks = new List<string>
{
    "C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/Disk1",
    "C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/Disk2",
    "C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/Disk3"
};

disks = disks.Select(x => Path.Combine(x, Path.GetRandomFileName())).ToList();

var fileStream = File.Open("C:/Users/thanh/Downloads/469.tif", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

using SHA256 sha256 = SHA256.Create();
int readByte = 0;
int totalBytesRead1 = 0;
byte[] buffer1 = new byte[1024];
byte[] buffer2 = new byte[1024];


var outPutStream = new FileStream("C:/Users/thanh/Downloads/output.tif", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024);

int tripSize = 1024;
var totalByteRead = await WriteDataAsync(fileStream, tripSize, disks[0], disks[1], disks[2]);
Console.WriteLine($"Total bytes read: {totalByteRead:N0}");
await ReadDataWithRecoveryAsync(outPutStream, tripSize, totalByteRead, disks[0], disks[1], "disks[2]");
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