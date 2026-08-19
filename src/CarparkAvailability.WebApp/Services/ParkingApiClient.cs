using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CarparkAvailability.WebApp.Services;

public enum AvailabilityStatus { Fresh, Stale, Unavailable }

public sealed record LotAvailability(
    [property: JsonPropertyName("lotType")] string LotType,
    [property: JsonPropertyName("totalLots")] int TotalLots,
    [property: JsonPropertyName("lotsAvailable")] int LotsAvailable,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt);

public sealed record CarparkResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("carparkType")] string CarparkType,
    [property: JsonPropertyName("typeOfParkingSystem")] string TypeOfParkingSystem,
    [property: JsonPropertyName("shortTermParking")] string ShortTermParking,
    [property: JsonPropertyName("freeParking")] string FreeParking,
    [property: JsonPropertyName("nightParking")] string NightParking,
    [property: JsonPropertyName("carparkDecks")] int CarparkDecks,
    [property: JsonPropertyName("gantryHeight")] double GantryHeight,
    [property: JsonPropertyName("hasBasement")] bool HasBasement,
    [property: JsonPropertyName("distanceMetres")] double DistanceMetres,
    [property: JsonPropertyName("lots")] IReadOnlyList<LotAvailability> Lots,
    [property: JsonPropertyName("availabilityStatus")] AvailabilityStatus AvailabilityStatus)
{
    public int CarLots => Lots.Where(l => l.LotType == "C").Sum(l => l.LotsAvailable);
    public int TotalCarLots => Lots.Where(l => l.LotType == "C").Sum(l => l.TotalLots);
    public int MotorcycleLots => Lots.Where(l => l.LotType == "Y").Sum(l => l.LotsAvailable);
    public int HeavyLots => Lots.Where(l => l.LotType == "H").Sum(l => l.LotsAvailable);
    public int OccupancyPercent => TotalCarLots > 0 ? (int)Math.Round((1.0 - (double)CarLots / TotalCarLots) * 100) : 0;
}

public sealed record DataStatusResponse(
    [property: JsonPropertyName("lastFetched")] DateTimeOffset? LastFetched,
    [property: JsonPropertyName("status")] AvailabilityStatus Status,
    [property: JsonPropertyName("totalStaticRecords")] int TotalStaticRecords,
    [property: JsonPropertyName("liveRecordsAvailable")] int LiveRecordsAvailable);

public sealed class ParkingApiClient(HttpClient httpClient)
{
    public async Task<List<CarparkResult>?> GetNearbyAsync(
        double lat, double lng, double radius = 500,
        bool? shortTerm = null, bool? freeParking = null, bool? nightParking = null,
        CancellationToken cancellationToken = default)
    {
        var qs = new System.Text.StringBuilder($"api/carparks/nearby?lat={lat}&lng={lng}&radius={radius}");
        if (shortTerm.HasValue) qs.Append($"&shortTerm={shortTerm.Value}");
        if (freeParking.HasValue) qs.Append($"&freeParking={freeParking.Value}");
        if (nightParking.HasValue) qs.Append($"&nightParking={nightParking.Value}");

        try
        {
            return await httpClient.GetFromJsonAsync<List<CarparkResult>>(qs.ToString(), cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<CarparkResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<CarparkResult>($"api/carparks/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DataStatusResponse?> GetDataStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DataStatusResponse>("api/data-status", cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
