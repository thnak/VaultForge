using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BrainNet.Utils;

public static class ImageExtensions
{
    public static void AutoOrient(this Image image) => image.Mutate(x => x.AutoOrient());

    public static Image<TPixel> As<TPixel>(this Image image) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (image is Image<TPixel> result)
        {
            return result;
        }

        return image.CloneAs<TPixel>();
    }

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

    public static DenseTensor<float> MatToDenseTensor(Mat mat)
    {
        int height = mat.Rows;
        int width = mat.Cols;
        int channels = mat.Channels();

        // Convert Mat to a float array
        float[] data = new float[height * width * channels];
        mat.GetArray(out Vec3d[] array);

        // Create a DenseTensor with NHWC format
        DenseTensor<float> tensor = new DenseTensor<float>(data, new[] { 1, height, width, channels });

        return tensor;
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
            var content = new StringContent(jsonPayload, Encoding.UTF8, MediaTypeNames.Application.Json);

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