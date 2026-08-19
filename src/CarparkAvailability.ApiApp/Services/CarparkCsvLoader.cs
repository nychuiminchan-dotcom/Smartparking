using CarparkAvailability.ApiApp.Models;
using Microsoft.Extensions.Options;

namespace CarparkAvailability.ApiApp.Services;

public sealed class CarparkCsvLoader(IOptions<StaticDataOptions> options, ILogger<CarparkCsvLoader> logger)
{
    private readonly string _csvPath = options.Value.CsvPath;

    private static readonly string[] RequiredHeaders =
    [
        "car_park_no", "address", "x_coord", "y_coord",
        "car_park_type", "type_of_parking_system",
        "short_term_parking", "free_parking", "night_parking",
        "car_park_decks", "gantry_height", "car_park_basement"
    ];

    public IReadOnlyList<CarparkStaticRecord> Load()
    {
        if (!File.Exists(_csvPath))
        {
            logger.LogError("HDB CSV not found at {Path}", _csvPath);
            throw new InvalidOperationException($"CSV file not found: {_csvPath}");
        }

        using var reader = new StreamReader(_csvPath);
        string? headerLine = reader.ReadLine();
        if (headerLine is null)
            throw new InvalidOperationException("CSV file is empty.");

        var headers = headerLine.Split(',');
        ValidateHeaders(headers);

        var index = BuildHeaderIndex(headers);
        var records = new List<CarparkStaticRecord>();
        int lineNumber = 1;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitCsvLine(line);
            if (cols.Length < headers.Length)
            {
                logger.LogWarning("Skipping malformed row at line {Line}", lineNumber);
                continue;
            }

            string carparkNo = cols[index["car_park_no"]].Trim();
            if (string.IsNullOrWhiteSpace(carparkNo))
            {
                logger.LogWarning("Skipping row with empty car_park_no at line {Line}", lineNumber);
                continue;
            }

            if (!TryParseDouble(cols[index["x_coord"]], out double x) ||
                !TryParseDouble(cols[index["y_coord"]], out double y))
            {
                logger.LogWarning("Skipping row {CarparkNo}: invalid coordinates at line {Line}", carparkNo, lineNumber);
                continue;
            }

            (double lat, double lon) = Svy21Converter.ToWgs84(x, y);

            if (!TryParseInt(cols[index["car_park_decks"]], out int decks)) decks = 0;
            if (!TryParseDouble(cols[index["gantry_height"]], out double gantry)) gantry = 0;

            string basement = cols[index["car_park_basement"]].Trim();

            records.Add(new CarparkStaticRecord(
                CarparkNo: carparkNo,
                Address: cols[index["address"]].Trim(),
                Latitude: lat,
                Longitude: lon,
                CarparkType: cols[index["car_park_type"]].Trim(),
                TypeOfParkingSystem: cols[index["type_of_parking_system"]].Trim(),
                ShortTermParking: cols[index["short_term_parking"]].Trim(),
                FreeParking: cols[index["free_parking"]].Trim(),
                NightParking: cols[index["night_parking"]].Trim(),
                CarparkDecks: decks,
                GantryHeight: gantry,
                HasBasement: basement.Equals("Y", StringComparison.OrdinalIgnoreCase)));
        }

        logger.LogInformation("Loaded {Count} carpark static records from CSV", records.Count);
        return records;
    }

    private static void ValidateHeaders(string[] headers)
    {
        var headerSet = headers.Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
        var missing = RequiredHeaders.Where(r => !headerSet.Contains(r)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"CSV missing required headers: {string.Join(", ", missing)}");
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers) =>
        headers.Select((h, i) => (h.Trim().ToLowerInvariant(), i))
               .ToDictionary(t => t.Item1, t => t.i);

    private static string[] SplitCsvLine(string line)
    {
        // Simple CSV split - handles basic quoting
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return [.. result];
    }

    private static bool TryParseDouble(string s, out double value) =>
        double.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);

    private static bool TryParseInt(string s, out int value) =>
        int.TryParse(s.Trim(), out value);
}

public sealed class StaticDataOptions
{
    public string CsvPath { get; set; } = "Data/HDBCarparkInformation.csv";
}
