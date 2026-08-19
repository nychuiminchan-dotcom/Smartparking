using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.ApiApp.Endpoints;

public static class CarparkEndpoints
{
    private const double EarthRadiusMetres = 6_371_000.0;

    public static IEndpointRouteBuilder MapCarparkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/carparks");

        group.MapGet("/nearby", GetNearby)
             .WithName("GetNearbyCarparks")
             .WithSummary("Search for carparks within a given radius of a location");

        group.MapGet("/{id}", GetById)
             .WithName("GetCarparkById")
             .WithSummary("Get details and availability for a specific carpark");

        app.MapGet("/api/data-status", GetDataStatus)
           .WithName("GetDataStatus")
           .WithSummary("Get the current data freshness status");

        return app;
    }

    private static IResult GetNearby(
        double lat,
        double lng,
        double radius = 500,
        string? vehicle = null,
        bool? shortTerm = null,
        bool? freeParking = null,
        bool? nightParking = null,
        CarparkRepository repo = default!)
    {
        if (lat < 1.1 || lat > 1.5 || lng < 103.5 || lng > 104.2)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["location"] = ["Coordinates must be within Singapore (lat 1.1-1.5, lng 103.5-104.2)."]
            });

        if (radius is <= 0 or > 5000)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["radius"] = ["Radius must be between 1 and 5000 metres."]
            });

        var status = repo.CurrentStatus;
        var results = new List<CarparkResult>();

        foreach (var record in repo.StaticRecords)
        {
            double dist = HaversineMetres(lat, lng, record.Latitude, record.Longitude);
            if (dist > radius) continue;

            if (!PassesFilters(record, vehicle, shortTerm, freeParking, nightParking))
                continue;

            var live = repo.GetLiveData(record.CarparkNo);
            results.Add(BuildResult(record, live, dist, status));
        }

        results.Sort((a, b) => a.DistanceMetres.CompareTo(b.DistanceMetres));
        return Results.Ok(results);
    }

    private static IResult GetById(string id, CarparkRepository repo)
    {
        var record = repo.StaticRecords.FirstOrDefault(r =>
            r.CarparkNo.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (record is null)
            return Results.NotFound();

        var live = repo.GetLiveData(record.CarparkNo);
        var result = BuildResult(record, live, 0, repo.CurrentStatus);
        return Results.Ok(result);
    }

    private static IResult GetDataStatus(CarparkRepository repo) =>
        Results.Ok(new DataStatusResponse(
            LastFetched: repo.LastFetched,
            Status: repo.CurrentStatus,
            TotalStaticRecords: repo.StaticRecords.Count,
            LiveRecordsAvailable: repo.LiveRecordCount));

    private static bool PassesFilters(
        CarparkStaticRecord record,
        string? vehicle,
        bool? shortTerm,
        bool? freeParking,
        bool? nightParking)
    {
        if (shortTerm == true &&
            record.ShortTermParking.Equals("NO", StringComparison.OrdinalIgnoreCase))
            return false;

        if (freeParking == true &&
            record.FreeParking.Equals("NO", StringComparison.OrdinalIgnoreCase))
            return false;

        if (nightParking == true &&
            record.NightParking.Equals("NO", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static CarparkResult BuildResult(
        CarparkStaticRecord record,
        CarparkLiveData? live,
        double distance,
        AvailabilityStatus repoStatus)
    {
        IReadOnlyList<LotAvailability> lots =
            live?.Lots ?? [];

        return new CarparkResult(
            Id: record.CarparkNo,
            Address: record.Address,
            Latitude: record.Latitude,
            Longitude: record.Longitude,
            CarparkType: record.CarparkType,
            TypeOfParkingSystem: record.TypeOfParkingSystem,
            ShortTermParking: record.ShortTermParking,
            FreeParking: record.FreeParking,
            NightParking: record.NightParking,
            CarparkDecks: record.CarparkDecks,
            GantryHeight: record.GantryHeight,
            HasBasement: record.HasBasement,
            DistanceMetres: Math.Round(distance, 1),
            Lots: lots,
            AvailabilityStatus: live is null ? AvailabilityStatus.Unavailable : repoStatus);
    }

    private static double HaversineMetres(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMetres * c;
    }
}
