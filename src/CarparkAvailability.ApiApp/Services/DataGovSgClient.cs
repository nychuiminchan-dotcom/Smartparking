using System.Net.Http.Headers;
using System.Text.Json;
using CarparkAvailability.ApiApp.Models;
using Microsoft.Extensions.Options;

namespace CarparkAvailability.ApiApp.Services;

public sealed class DataGovSgOptions
{
    public string BaseUrl { get; set; } = "https://api.data.gov.sg/v1/";
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class DataGovSgClient(HttpClient httpClient, IOptions<DataGovSgOptions> options, ILogger<DataGovSgClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<DataGovSgResponse?> GetCarparkAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "transport/carpark-availability");
            string apiKey = options.Value.ApiKey;
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("x-api-key", apiKey);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("data.gov.sg returned {StatusCode}", response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DataGovSgResponse>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch carpark availability from data.gov.sg");
            return null;
        }
    }
}
