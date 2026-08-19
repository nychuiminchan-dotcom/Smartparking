namespace CarparkAvailability.ApiApp.Services;

/// <summary>
/// Converts SVY21 (Singapore's local coordinate system) to WGS84 (lat/lng).
/// Algorithm based on the Singapore Land Authority technical reference.
/// </summary>
public static class Svy21Converter
{
    // SVY21 projection parameters
    private const double A = 6378137.0;           // Semi-major axis (WGS84)
    private const double F = 1.0 / 298.257223563; // Flattening (WGS84)
    private const double OLat = 1.366666;          // Origin latitude (degrees)
    private const double OLon = 103.833333;        // Origin longitude (degrees)
    private const double No = 38744.572;           // False northing
    private const double Eo = 28001.642;           // False easting
    private const double K = 1.0;                  // Scale factor

    private static readonly double E2 = 2 * F - F * F;
    private static readonly double E = Math.Sqrt(E2);
    private static readonly double N = F / (2 - F);
    private static readonly double N2 = N * N;
    private static readonly double N3 = N * N * N;
    private static readonly double N4 = N * N * N * N;

    private static double RadToDeg(double rad) => rad * 180.0 / Math.PI;
    private static double DegToRad(double deg) => deg * Math.PI / 180.0;

    public static (double Latitude, double Longitude) ToWgs84(double easting, double northing)
    {
        double oLatRad = DegToRad(OLat);
        double oLonRad = DegToRad(OLon);

        double Mo = MeridianArcLength(oLatRad);

        double M = Mo + (northing - No) / K;
        double sigma = M / (A * (1 - E2) * GA0);

        // Footpoint latitude by iteration
        double phi = sigma;
        for (int i = 0; i < 10; i++)
        {
            phi = sigma + C1 * Math.Sin(2 * phi)
                        - C2 * Math.Sin(4 * phi)
                        + C3 * Math.Sin(6 * phi)
                        - C4 * Math.Sin(8 * phi);
        }

        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        double tanPhi = Math.Tan(phi);

        double nu = A / Math.Sqrt(1 - E2 * sinPhi * sinPhi);
        double rho = A * (1 - E2) / Math.Pow(1 - E2 * sinPhi * sinPhi, 1.5);
        double psi = nu / rho;

        double x = easting - Eo;
        double D = x / (nu * K);

        // Latitude terms
        double lat = phi
            - (nu * tanPhi / rho)
              * (D * D / 2
                 - D * D * D * D / 24 * (5 + 3 * tanPhi * tanPhi + psi * (1 - 9 * tanPhi * tanPhi) - 4 * psi * psi));

        // Longitude terms
        double lon = oLonRad
            + (1 / cosPhi)
              * (D
                 - D * D * D / 6 * (psi + 2 * tanPhi * tanPhi)
                 + D * D * D * D * D / 120 * (5 + 28 * tanPhi * tanPhi + 24 * Math.Pow(tanPhi, 4) + psi * (6 + 8 * tanPhi * tanPhi)));

        return (RadToDeg(lat), RadToDeg(lon));
    }

    private static double MeridianArcLength(double lat)
    {
        double A0 = 1 - E2 / 4 - 3 * E2 * E2 / 64 - 5 * E2 * E2 * E2 / 256;
        double A2 = 3.0 / 8 * (E2 + E2 * E2 / 4 + 15 * E2 * E2 * E2 / 128);
        double A4 = 15.0 / 256 * (E2 * E2 + 3 * E2 * E2 * E2 / 4);
        double A6 = 35.0 * E2 * E2 * E2 / 3072;
        return A * (A0 * lat - A2 * Math.Sin(2 * lat) + A4 * Math.Sin(4 * lat) - A6 * Math.Sin(6 * lat));
    }

    private static readonly double GA0 = 1 - E2 / 4 - 3 * E2 * E2 / 64 - 5 * E2 * E2 * E2 / 256;

    // Series coefficients for footpoint latitude
    private static readonly double C1 = 3.0 / 2 * N - 27.0 / 32 * N3;
    private static readonly double C2 = 21.0 / 16 * N2 - 55.0 / 32 * N4;
    private static readonly double C3 = 151.0 / 96 * N3;
    private static readonly double C4 = 1097.0 / 512 * N4;
}
