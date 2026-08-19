using System.Text.Json.Serialization;

namespace CarparkAvailability.ApiApp.Models;

public sealed class DataGovSgResponse
{
    [JsonPropertyName("items")]
    public List<DataGovSgItem> Items { get; set; } = [];
}

public sealed class DataGovSgItem
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("carpark_data")]
    public List<DataGovSgCarparkData> CarparkData { get; set; } = [];
}

public sealed class DataGovSgCarparkData
{
    [JsonPropertyName("carpark_number")]
    public string CarparkNumber { get; set; } = string.Empty;

    [JsonPropertyName("update_datetime")]
    public string UpdateDatetime { get; set; } = string.Empty;

    [JsonPropertyName("carpark_info")]
    public List<DataGovSgCarparkInfo> CarparkInfo { get; set; } = [];
}

public sealed class DataGovSgCarparkInfo
{
    [JsonPropertyName("lot_type")]
    public string LotType { get; set; } = string.Empty;

    [JsonPropertyName("total_lots")]
    public string TotalLots { get; set; } = string.Empty;

    [JsonPropertyName("lots_available")]
    public string LotsAvailable { get; set; } = string.Empty;
}
