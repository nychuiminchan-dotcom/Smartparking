using CarparkAvailability.ApiApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CarparkAvailability.Tests;

public class CarparkCsvLoaderTests
{
    private static CarparkCsvLoader CreateLoader(string csvPath) =>
        new(Options.Create(new StaticDataOptions { CsvPath = csvPath }),
            NullLogger<CarparkCsvLoader>.Instance);

    [Fact]
    public void Load_ValidCsv_ReturnsRecords()
    {
        string path = WriteTempCsv(
            "car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement",
            "ACB,ALBERT CENTRE,30314.7936,31490.4942,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,1,1.8,Y",
            "ACM,ALJUNIED CRESCENT,33758.4143,33695.5198,MULTI-STOREY CAR PARK,ELECTRONIC PARKING,WHOLE DAY,SUN & PH FR 7AM-10.30PM,YES,5,2.1,N");

        var loader = CreateLoader(path);
        var records = loader.Load();

        Assert.Equal(2, records.Count);
        Assert.Equal("ACB", records[0].CarparkNo);
        Assert.Equal("ALBERT CENTRE", records[0].Address);
        Assert.True(records[0].HasBasement);
        Assert.False(records[1].HasBasement);
        Assert.InRange(records[0].Latitude, 1.1, 1.5);
        Assert.InRange(records[0].Longitude, 103.5, 104.2);
    }

    [Fact]
    public void Load_SkipsRowWithInvalidCoordinates()
    {
        string path = WriteTempCsv(
            "car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement",
            "ACB,VALID,30314.7936,31490.4942,BASEMENT,ELECTRONIC,WHOLE DAY,NO,YES,1,1.8,Y",
            "BAD,INVALID COORDS,notanumber,notanumber,SURFACE,COUPON,WHOLE DAY,NO,NO,0,0,N");

        var records = CreateLoader(path).Load();

        Assert.Single(records);
        Assert.Equal("ACB", records[0].CarparkNo);
    }

    [Fact]
    public void Load_MissingRequiredHeader_Throws()
    {
        string path = WriteTempCsv(
            "car_park_no,address,x_coord,y_coord",  // missing required headers
            "ACB,TEST,30314.0,31490.0");

        Assert.Throws<InvalidOperationException>(() => CreateLoader(path).Load());
    }

    [Fact]
    public void Load_EmptyFile_Throws()
    {
        string path = Path.GetTempFileName();
        Assert.Throws<InvalidOperationException>(() => CreateLoader(path).Load());
    }

    [Fact]
    public void Load_FileNotFound_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreateLoader("/does/not/exist.csv").Load());
    }

    private static string WriteTempCsv(params string[] lines)
    {
        string path = Path.GetTempFileName();
        File.WriteAllLines(path, lines);
        return path;
    }
}
