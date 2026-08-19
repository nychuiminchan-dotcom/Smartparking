using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.Tests;

public class Svy21ConverterTests
{
    // SVY21 False origin: easting=28001.642, northing=38744.572 → should map back to origin lat/lng
    [Fact]
    public void FalseOrigin_MapsToOriginLatLng()
    {
        var (lat, lon) = Svy21Converter.ToWgs84(28001.642, 38744.572);

        // Should be close to the SVY21 origin (1.367°N, 103.833°E)
        Assert.InRange(lat, 1.36, 1.38);
        Assert.InRange(lon, 103.82, 103.85);
    }

    [Theory]
    [InlineData(30314.7936, 31490.4942)]  // ACB - Albert Centre area
    [InlineData(33758.4143, 33695.5198)]  // ACM - Aljunied area
    [InlineData(28185.4359, 39012.6664)]  // AK19 - Ang Mo Kio
    [InlineData(29674.8184, 40616.875)]   // AK9 - Ang Mo Kio
    public void KnownCsvCoordinates_AreWithinSingaporeBounds(double x, double y)
    {
        var (lat, lon) = Svy21Converter.ToWgs84(x, y);

        Assert.InRange(lat, 1.1, 1.5);
        Assert.InRange(lon, 103.5, 104.2);
    }
}
