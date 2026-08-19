using System.Collections.Concurrent;
using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

/// <summary>
/// Holds the joined static + live data. Refreshes live availability on a timer.
/// </summary>
public sealed class CarparkRepository : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    private readonly CarparkCsvLoader _csvLoader;
    private readonly DataGovSgClient _dataGovSgClient;
    private readonly ILogger<CarparkRepository> _logger;

    private IReadOnlyList<CarparkStaticRecord> _staticRecords = [];
    private ConcurrentDictionary<string, CarparkLiveData> _liveData = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? _lastFetched;
    private bool _everFetched;

    public CarparkRepository(
        CarparkCsvLoader csvLoader,
        DataGovSgClient dataGovSgClient,
        ILogger<CarparkRepository> logger)
    {
        _csvLoader = csvLoader;
        _dataGovSgClient = dataGovSgClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load static data once on startup
        try
        {
            _staticRecords = _csvLoader.Load();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load HDB carpark CSV data");
            throw;
        }

        // Immediately fetch live data, then repeat
        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchLiveDataAsync(stoppingToken);
            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task FetchLiveDataAsync(CancellationToken cancellationToken)
    {
        var response = await _dataGovSgClient.GetCarparkAvailabilityAsync(cancellationToken);
        if (response?.Items is { Count: > 0 } items)
        {
            var newLive = new ConcurrentDictionary<string, CarparkLiveData>(StringComparer.OrdinalIgnoreCase);
            var fetchTime = DateTime.UtcNow;
            var item = items[0]; // Latest snapshot

            foreach (var cp in item.CarparkData)
            {
                if (string.IsNullOrWhiteSpace(cp.CarparkNumber)) continue;

                var lots = cp.CarparkInfo
                    .Where(i => !string.IsNullOrWhiteSpace(i.LotType))
                    .Select(i =>
                    {
                        int.TryParse(i.TotalLots, out int total);
                        int.TryParse(i.LotsAvailable, out int available);
                        DateTime.TryParse(cp.UpdateDatetime, out DateTime updated);
                        return new LotAvailability(i.LotType, total, available, updated == default ? fetchTime : updated);
                    })
                    .ToList();

                newLive[cp.CarparkNumber] = new CarparkLiveData(cp.CarparkNumber, lots, fetchTime);
            }

            Interlocked.Exchange(ref _liveData, newLive);
            _lastFetched = DateTimeOffset.UtcNow;
            _everFetched = true;
            _logger.LogInformation("Refreshed live availability for {Count} carparks", newLive.Count);
        }
        else
        {
            _logger.LogWarning("Live availability fetch returned no data");
        }
    }

    public IReadOnlyList<CarparkStaticRecord> StaticRecords => _staticRecords;

    public AvailabilityStatus CurrentStatus
    {
        get
        {
            if (!_everFetched) return AvailabilityStatus.Unavailable;
            if (_lastFetched is null) return AvailabilityStatus.Unavailable;
            return DateTimeOffset.UtcNow - _lastFetched.Value > StaleThreshold
                ? AvailabilityStatus.Stale
                : AvailabilityStatus.Fresh;
        }
    }

    public DateTimeOffset? LastFetched => _lastFetched;
    public int LiveRecordCount => _liveData.Count;

    public CarparkLiveData? GetLiveData(string carparkNo) =>
        _liveData.TryGetValue(carparkNo, out var live) ? live : null;
}
