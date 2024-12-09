// using ConsoleApp1;
//
// int stripSize = 4096;
// FileStream fileSourceStream = new FileStream("C:/Users/thanh/OneDrive/Pictures/WallPaper/184069 (Original).mp4", FileMode.Open, FileAccess.Read);
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
// foreach (var path in paths)
// {
//     if (File.Exists(path))
//         File.Delete(path);
// }
//
// Raid5Stream raid5Stream = new Raid5Stream(paths, fileSourceStream.Length, 4096, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
//
// await raid5Stream.CopyFromAsync(fileSourceStream, (int)fileSourceStream.Length);
// await raid5Stream.FlushAsync();
//
// int seekPosition = 547839;
// raid5Stream.Seek(seekPosition, SeekOrigin.Begin);
// fileSourceStream.Seek(seekPosition, SeekOrigin.Begin);
//
// MemoryStream raid5OutputStream = new MemoryStream();
// await raid5Stream.CopyToAsync(raid5OutputStream);
// raid5OutputStream.Seek(0, SeekOrigin.Begin);
//
// MemoryStream sourceOutputStream = new MemoryStream();
// await fileSourceStream.CopyToAsync(sourceOutputStream);
// sourceOutputStream.Seek(0, SeekOrigin.Begin);
//
// // var image = await Image.LoadAsync(outputStream);
//
// var isTheSame = raid5OutputStream.CompareHashes(sourceOutputStream);
// Console.WriteLine(isTheSame);
// await raid5OutputStream.Compare(sourceOutputStream);

// Import packages
//
// using System.Text;
// using ConsoleApp1;
// using Microsoft.SemanticKernel;
// using Microsoft.SemanticKernel.ChatCompletion;
// using Microsoft.SemanticKernel.Connectors.Ollama;
//
//
// // Create a kernel with Azure OpenAI chat completion
// #pragma warning disable SKEXP0070
// var builder = Kernel.CreateBuilder();
// builder.AddOllamaChatCompletion("llama3.1", new Uri("http://thnakdevserver.ddns.net:11434"), "chat-with-ollama");
// // builder.AddGoogleAIGeminiChatCompletion("gemini-1.5-flash-latest", "AIzaSyB58jzpB0N6cNhe-urq32CvgDV6p39FC1A");
// builder.Plugins.AddFromType<LightsPlugin>("Lights");
// #pragma warning restore SKEXP0070
//
// // Build the kernel
// Kernel kernel = builder.Build();
// IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
//
// // Manual function invocation needs to be enabled explicitly by setting autoInvoke to false.
// #pragma warning disable SKEXP0070
// OllamaPromptExecutionSettings settings = new()
// #pragma warning restore SKEXP0070
// {
//     FunctionChoiceBehavior = null
// };
//
// ChatHistory chatHistory = [];
// chatHistory.AddSystemMessage("code assistant");
// while (true)
// {
//     Console.Write("User: ");
//     string? userInput = Console.ReadLine();
//     if (userInput == null)
//         break;
//     
//     chatHistory.AddUserMessage(userInput);
//     AuthorRole? authorRole = null;
//     FunctionCallContentBuilder fccBuilder = new ();
//
//     StringBuilder builderResponse = new(); 
//     // Start or continue streaming chat based on the chat history
//     await foreach (StreamingChatMessageContent streamingContent in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel))
//     {
//         // Check if the AI model has generated a response.
//         if (streamingContent.Content is not null)
//         {
//             Console.Write(streamingContent.Content);
//             // Sample streamed output: "The color of the sky in Boston is likely to be gray due to the rainy weather."
//         }
//         authorRole ??= streamingContent.Role;
//
//         // Collect function calls details from the streaming content
//         fccBuilder.Append(streamingContent);
//         builderResponse.Append(streamingContent.Content);
//     }
//
//     Console.WriteLine();
//     // Build the function calls from the streaming content and quit the chat loop if no function calls are found
//     IReadOnlyList<FunctionCallContent> functionCalls = fccBuilder.Build();
//     if (!functionCalls.Any())
//     {
//         chatHistory.Add(new ChatMessageContent(role: authorRole ?? default, content: builderResponse.ToString()));
//         continue;
//     }
//
//     // Creating and adding chat message content to preserve the original function calls in the chat history.
//     // The function calls are added to the chat message a few lines below.
//     ChatMessageContent fcContent = new ChatMessageContent(role: authorRole ?? default, content: null);
//     chatHistory.Add(fcContent);
//
//     // Iterating over the requested function calls and invoking them.
//     // The code can easily be modified to invoke functions concurrently if needed.
//     foreach (FunctionCallContent functionCall in functionCalls)
//     {
//         // Adding the original function call to the chat message content
//         fcContent.Items.Add(functionCall);
//
//         // Invoking the function
//         FunctionResultContent functionResult = await functionCall.InvokeAsync(kernel);
//
//         // Adding the function result to the chat history
//         chatHistory.Add(functionResult.ToChatMessage());
//     }
// }

 List<string> ChunkText(string text, int chunkSize = 500, int overlap = 100)
{
    List<string> chunks = new List<string>();
    int startIndex = 0;

    while (startIndex < text.Length)
    {
        int endIndex = Math.Min(startIndex + chunkSize, text.Length);
        string chunk = text.Substring(startIndex, endIndex - startIndex);

        chunks.Add(chunk);

        // Calculate the next start index, considering the overlap
        startIndex += chunkSize - overlap;
    }

    return chunks;
}

var text = ChunkText("Your long input text goes here. This function will split the text into manageable chunks. Each chunk will be of specified size", 100, 10);
Console.WriteLine(text);

//
// DateTime MinDate = new DateTime(1970, 1, 1, 0, 0, 0);
//
// DateOnly GetDateOnlyFromUnixDay(int day)
// {
//     var dateOnly = DateOnly.FromDateTime(MinDate);
//     return dateOnly.AddDays(day);
// }
//
//
// var date = GetDateOnlyFromUnixDay(20061);
// Console.WriteLine();

// using BrainNet.Service.FaceEmbedding.Implements;
//
// using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/Downloads/Facenet512.onnx");
// string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
// string folderPath = "C:/Users/thanh/Downloads/archive/Faces/Faces";
//
// var imageFiles = new List<string>();
// foreach (var fileImage in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower())))
// {
//     imageFiles.Add(fileImage);
// }
//
//
// var fileGroupByName = imageFiles.GroupBy(image => Path.GetFileNameWithoutExtension(image).Split("_").First());
// foreach (var fileGroup in fileGroupByName)
// {
//     foreach (var file in fileGroup)
//     {
//         faceEmbedding.GetEmbeddingArray(file);
//
//     }
// }
//