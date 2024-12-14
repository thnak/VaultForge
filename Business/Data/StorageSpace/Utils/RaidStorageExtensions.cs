using System.Security.Cryptography;
using Business.Utils.Protector;
using BusinessModels.General.Results;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using MongoDB.Bson;

namespace Business.Data.StorageSpace.Utils;

public static class RaidStorageExtensions
{
    public static bool ValidateStorageFormat(this IEnumerable<string> disks, RaidType raidType)
    {
        switch (raidType)
        {
            case RaidType.Raid5:
            {
                if (disks.Count() >= 3) return true;
                break;
            }
            case RaidType.Raid6:
            {
                if (disks.Count() >= 4) return true;
                break;
            }
            case RaidType.Raid0:
            {
                if (disks.Any()) return true;
                break;
            }
            case RaidType.Raid1:
            {
                if (disks.Count() >= 2) return true;
                break;
            }
        }

        return false;
    }

    public static List<FileRaidDataBlockModel> GetShuffledDisks(this IEnumerable<string> fileFolders, ObjectId raidId)
    {
        string[] arrayDisk = fileFolders.ToArray();
        arrayDisk.Shuffle();
        int index = 0;

        using var sha256 = SHA256.Create();
        return arrayDisk.Select(x => new FileRaidDataBlockModel
        {
            AbsolutePath = Path.Combine(x, sha256.GenerateAliasKey(raidId, Guid.NewGuid().ToString())),
            CreationTime = DateTime.UtcNow,
            ModificationTime = DateTime.UtcNow,
            RelativePath = raidId.ToString(),
            Index = index++
        }).ToList();
    }

    public static void InitPhysicFolder(this IEnumerable<FileRaidDataBlockModel> files)
    {
        foreach (var disk in files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(disk.AbsolutePath)!);
        }
    }

    public static void DeletePhysicFiles(this IEnumerable<string> filePaths)
    {
        foreach (var file in filePaths)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    /// <summary>
    /// try to open file,
    /// </summary>
    /// <param name="path"></param>
    /// <param name="bufferSize"></param>
    /// <returns></returns>
    public static Result<FileStream?> OpenFileWrite(this string path, int bufferSize)
    {
        FileStream? file1;
        if (File.Exists(path))
            return Result<FileStream?>.Failure("File already exists", ErrorType.NotFound);
        try
        {
            file1 = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: bufferSize, useAsync: true);
        }
        catch (IOException exception)
        {
            return Result<FileStream?>.Failure(exception.Message, ErrorType.PermissionDenied);
        }
        catch (Exception exception)
        {
            return Result<FileStream?>.Failure(exception.Message, ErrorType.Unknown);
        }

        return Result<FileStream?>.Success(file1);
    }
}