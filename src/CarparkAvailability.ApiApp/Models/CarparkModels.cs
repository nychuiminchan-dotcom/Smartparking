namespace CarparkAvailability.ApiApp.Models;

public sealed record CarparkStaticRecord(
    string CarparkNo,
    string Address,
    double Latitude,
    double Longitude,
    string CarparkType,
    string TypeOfParkingSystem,
    string ShortTermParking,
    string FreeParking,
    string NightParking,
    int CarparkDecks,
    double GantryHeight,
    bool HasBasement);

public enum AvailabilityStatus { Fresh, Stale, Unavailable }

public sealed record LotAvailability(
    string LotType,
    int TotalLots,
    int LotsAvailable,
    DateTime UpdatedAt);

public sealed record CarparkLiveData(
    string CarparkNo,
    IReadOnlyList<LotAvailability> Lots,
    DateTime FetchedAt);

public sealed record CarparkResult(
    string Id,
    string Address,
    double Latitude,
    double Longitude,
    string CarparkType,
    string TypeOfParkingSystem,
    string ShortTermParking,
    string FreeParking,
    string NightParking,
    int CarparkDecks,
    double GantryHeight,
    bool HasBasement,
    double DistanceMetres,
    IReadOnlyList<LotAvailability> Lots,
    AvailabilityStatus AvailabilityStatus);

public sealed record DataStatusResponse(
    DateTimeOffset? LastFetched,
    AvailabilityStatus Status,
    int TotalStaticRecords,
    int LiveRecordsAvailable);
