using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Utils;

public static class ImageExtensions
{
    public static DenseTensor<float> Image2DenseTensor(Image<Rgb24> image)
    {
        int[] shape = new[] { 3, image.Height, image.Width };

        DenseTensor<float> feed = new DenseTensor<float>(shape);

        Parallel.For(0, shape[1], y =>
        {
            for (var x = 0; x < shape[2]; x++)
            {
                feed[0, y, x] = image[x, y].R;
                feed[1, y, x] = image[x, y].G;
                feed[2, y, x] = image[x, y].B;
            }
        });

        return feed;
    }

    public static async Task<string> GenerateDescription(this MemoryStream stream, string connectionString, string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = stream.ToArray();
            var base64 = Convert.ToBase64String(data);
            HttpClient httpClient = new();
            var payload = new
            {
                model = modelName,
                prompt = "What is in this picture?",
                stream = false,
                images = new[] { base64 } // empty array for images
            };
            string jsonPayload = JsonSerializer.Serialize(payload);

            // Create HttpContent from the JSON payload
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(connectionString, content, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var message = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonResponse = JObject.Parse(message);
                string responseContent = jsonResponse["response"]?.ToString() ?? string.Empty;
                return responseContent;
            }

            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}