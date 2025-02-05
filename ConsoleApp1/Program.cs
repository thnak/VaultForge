﻿// using ConsoleApp1;
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


using System.Text;
using ConsoleApp1;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;


// Create a kernel with Azure OpenAI chat completion
#pragma warning disable SKEXP0070
var builder = Kernel.CreateBuilder();
builder.AddOllamaChatCompletion("hengwen/watt-tool-8B", new Uri("http://14.169.232.112:11434"), "chat-with-ollama");
// builder.AddGoogleAIGeminiChatCompletion("gemini-1.5-flash-latest", "AIzaSyB58jzpB0N6cNhe-urq32CvgDV6p39FC1A");
builder.Plugins.AddFromType<LightsPlugin>("Lights");
#pragma warning restore SKEXP0070

// Build the kernel
Kernel kernel = builder.Build();
IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Manual function invocation needs to be enabled explicitly by setting autoInvoke to false.
#pragma warning disable SKEXP0070
OllamaPromptExecutionSettings settings = new()
#pragma warning restore SKEXP0070
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

ChatHistory chatHistory = [];    
chatHistory.AddSystemMessage("""
                             You are an expert in composing functions. You are given a question and a set of possible functions. Based on the question, you will need to make one or more function/tool calls to achieve the purpose.
                             If none of the function can be used, point it out. If the given question lacks the parameters required by the function, also point it out.
                             You should only return the function call in tools call sections.
                             
                             If you decide to invoke any of the function(s), you MUST put it in the format of [func_name1(params_name1=params_value1, params_name2=params_value2...), func_name2(params)]
                             You SHOULD NOT include any other text in the response.
                             Here is a list of functions in JSON format that you can invoke.\n{functions}\n
                             """);
while (true)
{
    Console.Write("User: ");
    string? userInput = Console.ReadLine();
    if (userInput == null)
        break;
    
    chatHistory.AddUserMessage(userInput);
    AuthorRole? authorRole = null;
    FunctionCallContentBuilder fccBuilder = new ();

    
    var response = await chatCompletionService.GetChatMessageContentsAsync(chatHistory);
    foreach (var chat in response)
    {
        chatHistory.Add(chat);
    }
    continue;
    
    StringBuilder builderResponse = new(); 
    // Start or continue streaming chat based on the chat history
    await foreach (StreamingChatMessageContent streamingContent in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel))
    {
        // Check if the AI model has generated a response.
        if (streamingContent.Content is not null)
        {
            Console.Write(streamingContent.Content);
            // Sample streamed output: "The color of the sky in Boston is likely to be gray due to the rainy weather."
        }
        authorRole ??= streamingContent.Role;

        // Collect function calls details from the streaming content
        fccBuilder.Append(streamingContent);
        builderResponse.Append(streamingContent.Content);
    }

    Console.WriteLine();
    // Build the function calls from the streaming content and quit the chat loop if no function calls are found
    IReadOnlyList<FunctionCallContent> functionCalls = fccBuilder.Build();
    if (!functionCalls.Any())
    {
        chatHistory.Add(new ChatMessageContent(role: authorRole ?? default, content: builderResponse.ToString()));
        continue;
    }

    // Creating and adding chat message content to preserve the original function calls in the chat history.
    // The function calls are added to the chat message a few lines below.
    ChatMessageContent fcContent = new ChatMessageContent(role: authorRole ?? default, content: null);
    chatHistory.Add(fcContent);

    // Iterating over the requested function calls and invoking them.
    // The code can easily be modified to invoke functions concurrently if needed.
    foreach (FunctionCallContent functionCall in functionCalls)
    {
        // Adding the original function call to the chat message content
        fcContent.Items.Add(functionCall);

        // Invoking the function
        FunctionResultContent functionResult = await functionCall.InvokeAsync(kernel);

        // Adding the function result to the chat history
        chatHistory.Add(functionResult.ToChatMessage());
    }
}

// using BenchmarkDotNet.Attributes;
// using BenchmarkDotNet.Columns;
// using BenchmarkDotNet.Configs;
// using BenchmarkDotNet.Loggers;
// using BenchmarkDotNet.Running;
// using BenchmarkDotNet.Validators;
// using BrainNet.Service.ObjectDetection;
// using BrainNet.Service.ObjectDetection.Implements;
// using BrainNet.Service.ObjectDetection.Model.Feeder;
// using BrainNet.Utils;
// using Microsoft.ML.OnnxRuntime.Tensors;
// using OpenCvSharp;
// using SixLabors.ImageSharp;
// using SixLabors.ImageSharp.PixelFormats;
//
// [Benchmark]
// DenseTensor<float> ResizeLinear(DenseTensor<float> imageMatrix, int[] shape)
// {
//     int[] newShape = [3, shape[0], shape[1]];
//     DenseTensor<float> outputImage = new DenseTensor<float>(newShape);
//
//     int originalHeight = imageMatrix.Dimensions[1];
//     int originalWidth = imageMatrix.Dimensions[2];
//
//     float invScaleFactorY = (float)originalHeight / shape[0];
//     float invScaleFactorX = (float)originalWidth / shape[1];
//
//     Parallel.For(0, shape[0], y =>
//     {
//         float oldY = y * invScaleFactorY;
//         int yFloor = (int)Math.Floor(oldY);
//         int yCeil = Math.Min(originalHeight - 1, (int)Math.Ceiling(oldY));
//         float yFraction = oldY - yFloor;
//
//         for (int x = 0; x < shape[1]; x++)
//         {
//             float oldX = x * invScaleFactorX;
//             int xFloor = (int)Math.Floor(oldX);
//             int xCeil = Math.Min(originalWidth - 1, (int)Math.Ceiling(oldX));
//             float xFraction = oldX - xFloor;
//
//             for (int c = 0; c < 3; c++)
//             {
//                 // Sample four neighboring pixels
//                 float topLeft = imageMatrix[c, yFloor, xFloor];
//                 float topRight = imageMatrix[c, yFloor, xCeil];
//                 float bottomLeft = imageMatrix[c, yCeil, xFloor];
//                 float bottomRight = imageMatrix[c, yCeil, xCeil];
//
//                 // Interpolate between the four neighboring pixels
//                 float topBlend = topLeft + xFraction * (topRight - topLeft);
//                 float bottomBlend = bottomLeft + xFraction * (bottomRight - bottomLeft);
//                 float finalBlend = topBlend + yFraction * (bottomBlend - topBlend);
//
//                 // Assign the interpolated value to the output image
//                 outputImage[c, y, x] = finalBlend;
//             }
//         }
//     });
//
//     return outputImage;
// }
//
// var config = new ManualConfig()
//     .WithOptions(ConfigOptions.DisableOptimizationsValidator)
//     .AddValidator(JitOptimizationsValidator.DontFailOnError)
//     .AddLogger(ConsoleLogger.Default)
//     .AddColumnProvider(DefaultColumnProviders.Instance)
//     .AddAnalyser();
//
// // BenchmarkRunner.Run<FunctionBenchmark>(config);
// string inputPath = "C:\\Users\\thanh\\OneDrive\\Pictures\\WallPaper\\ball-8048205.jpg";
// var yolo = new YoloDetection("C:\\Users\\thanh\\Downloads\\yolov7.onnx");
// var image = Image.Load<Rgb24>(inputPath);
// var result = yolo.PreprocessAndRun(image);
// var feed = new YoloFeeder(yolo.GetInputDimensions()[2..], yolo.GetStride());
// // feed.SetTensor(inputPath);
// // result = yolo.Predict(feed);
// var resultImage = image.PlotImage(yolo.PrimaryFont, result);
// resultImage.SaveAsJpeg("C:\\Users\\thanh\\OneDrive\\Pictures\\WallPaper\\detect\\ball-8048205.jpg");
// Console.WriteLine();

// public class FunctionBenchmark()
// {
//     readonly DenseTensor<float> _tensor1280 = new([3, 1280, 1280]);
//     readonly DenseTensor<float> _tensor640 = new([3, 640, 640]);
//     readonly Image<Rgb24> dummyImage = new Image<Rgb24>(1280, 1280);
//
//     private
//         readonly Mat dummy = new Mat(1280, 1280, MatType.CV_32FC3, Scalar.All(127.0));
    // [Benchmark]
    // public void RunResizeLinear()
    // {
    //     _tensor1280.ResizeLinear([3, 640, 640]);
    // }

    // [Benchmark]
    // public void AllocSpeed()
    // {
    //     DenseTensor<float> _tensor640 = new([3, 640, 640]);
    // }
    //
    // [Benchmark]
    // public void RunFill()
    // {
    //     _tensor640.Fill(114);
    // }
    //
    // [Benchmark]
    // public void RunCustomFill()
    // {
    //     _tensor640.FillTensor(114);
    // }

    // [Benchmark]
    // public void RunYoloLetterBox()
    // {
    //     dummy.Letterbox(new Size(640, 640));
    // }
    //
    // [Benchmark]
    // public void RunLetterBox()
    // {
    //     _tensor1280.LetterBox(false, false, true, 32, [640, 640]);
    // }
// }


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