using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Business.Services.RetrievalAugmentedGeneration.Implement;
using Business.Services.RetrievalAugmentedGeneration.Interface;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Business.Services.RetrievalAugmentedGeneration.Utils;

public static class RetrievalAugmentedGenerationExtension
{
    [Experimental("SKEXP0020")]
    public static void AddRetrievalAugmentedGeneration(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IMovieDatabase, MovieDatabase>();

        serviceCollection.AddHostedService<HostApplicationLifetimeEventsHostedService>();
    }

    public static async Task<string> GenerateDescription(MemoryStream stream, string connectionString, string modelName, CancellationToken cancellationToken = default)
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