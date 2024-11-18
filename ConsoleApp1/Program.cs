using ConsoleApp1;
using SixLabors.ImageSharp;

int stripSize = 4096;
FileStream memoryStream = new FileStream("C:/Users/thanh/OneDrive/Pictures/WallPaper/184069 (Original).mp4", FileMode.Open, FileAccess.Read);

string[] paths =
[
    "C:/Users/thanh/source/VitualDisk1/bin.bin",
    "C:/Users/thanh/source/VitualDisk2/bin.bin",
    "C:/Users/thanh/source/VitualDisk3/bin.bin",
    "C:/Users/thanh/source/VitualDisk4/bin.bin",
    "C:/Users/thanh/source/VitualDisk5/bin.bin",
    "C:/Users/thanh/source/VitualDisk6/bin.bin"
];

// foreach (var path in paths)
// {
//     if (File.Exists(path))
//         File.Delete(path);
// }

Raid5Stream stream = new Raid5Stream(paths, memoryStream.Length, 4096, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

// await stream.CopyFromAsync(memoryStream, (int)memoryStream.Length);
// await stream.FlushAsync();
// stream.Seek(0, SeekOrigin.Begin);

MemoryStream outputStream = new MemoryStream();
await stream.CopyToAsync(outputStream, (int)memoryStream.Length);
stream.Seek(0, SeekOrigin.Begin);

memoryStream.Seek(0, SeekOrigin.Begin);
outputStream.Seek(0, SeekOrigin.Begin);
// var image = await Image.LoadAsync(outputStream);

var isTheSame = outputStream.CompareHashes(memoryStream);
Console.WriteLine(isTheSame);
// await outputStream.Compare(memoryStream);