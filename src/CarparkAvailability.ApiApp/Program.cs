using CarparkAvailability.ApiApp.Endpoints;
using CarparkAvailability.ApiApp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Static data options
builder.Services.Configure<StaticDataOptions>(
    builder.Configuration.GetSection("StaticData"));

// data.gov.sg options and client
builder.Services.Configure<DataGovSgOptions>(
    builder.Configuration.GetSection("DataGovSg"));
builder.Services.AddHttpClient<DataGovSgClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataGovSgOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// CSV loader and repository
builder.Services.AddSingleton<CarparkCsvLoader>();
builder.Services.AddSingleton<CarparkRepository>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CarparkRepository>());

// CORS — allow WebApp origin (configured in WebApp:AllowedOrigins) or development wildcard
string[] allowedOrigins = builder.Configuration.GetSection("WebApp:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (allowedOrigins.Length > 0)
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    else if (builder.Environment.IsDevelopment())
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

WebApplication app = builder.Build();

app.UseCors();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCarparkEndpoints();
app.MapDefaultEndpoints();

app.Run();
