using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CarparkAvailability.WebApp.Services;

public sealed record NominatimResult(
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("lat")] string Lat,
    [property: JsonPropertyName("lon")] string Lon);

public sealed class NominatimService(HttpClient httpClient)
{
    public async Task<List<NominatimResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        string url = $"search?q={Uri.EscapeDataString(query + ", Singapore")}" +
                     "&format=json&limit=5&countrycodes=sg";
        try
        {
            var results = await httpClient.GetFromJsonAsync<List<NominatimResult>>(url, cancellationToken);
            return results ?? [];
        }
        catch
        {
            return [];
        }
    }
}
