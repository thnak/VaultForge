// using ConsoleApp1;
// using SixLabors.ImageSharp;
//
// int stripSize = 4096;
// FileStream memoryStream = new FileStream("C:/Users/thanh/OneDrive/Pictures/WallPaper/184069 (Original).mp4", FileMode.Open, FileAccess.Read);
//
// string[] paths =
// [
//     "C:/Users/thanh/source/VitualDisk1/bin.bin",
//     "C:/Users/thanh/source/VitualDisk2/bin.bin",
//     "C:/Users/thanh/source/VitualDisk3/bin.bin",
//     "C:/Users/thanh/source/VitualDisk4/bin.bin",
//     "C:/Users/thanh/source/VitualDisk5/bin.bin",
//     "C:/Users/thanh/source/VitualDisk6/bin.bin"
// ];
//
// // foreach (var path in paths)
// // {
// //     if (File.Exists(path))
// //         File.Delete(path);
// // }
//
// Raid5Stream stream = new Raid5Stream(paths, memoryStream.Length, 4096, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
//
// // await stream.CopyFromAsync(memoryStream, (int)memoryStream.Length);
// // await stream.FlushAsync();
// // stream.Seek(0, SeekOrigin.Begin);
//
// MemoryStream outputStream = new MemoryStream();
// await stream.CopyToAsync(outputStream, (int)memoryStream.Length);
// stream.Seek(0, SeekOrigin.Begin);
//
// memoryStream.Seek(0, SeekOrigin.Begin);
// outputStream.Seek(0, SeekOrigin.Begin);
// // var image = await Image.LoadAsync(outputStream);
//
// var isTheSame = outputStream.CompareHashes(memoryStream);
// Console.WriteLine(isTheSame);
// // await outputStream.Compare(memoryStream);

// Import packages

using ConsoleApp1;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


// Create a kernel with Azure OpenAI chat completion
#pragma warning disable SKEXP0070
var builder = Kernel.CreateBuilder().AddOllamaChatCompletion("llama3.2:1b", new Uri("http://localhost:11434/"));
#pragma warning restore SKEXP0070

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
// Add a plugin (the LightsPlugin class is defined below)
kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// Enable planning
PromptExecutionSettings openAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions()
    {
        AllowParallelCalls = true,
        AllowConcurrentInvocation = true
    })
};

// Create a history store the conversation
var history = new ChatHistory();

// Initiate a back-and-forth chat
string? userInput;
do
{
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();
    if (userInput is null) break;
    // Add user input
    history.AddUserMessage(userInput);

    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAiPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.WriteLine("Assistant > " + result);

    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);