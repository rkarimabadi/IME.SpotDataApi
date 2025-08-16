using IME.SpotDataApi.Data;
using IME.SpotDataApi.Helpers;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Authenticate;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Models.Notification;
using IME.SpotDataApi.Repository;
using IME.SpotDataApi.Services.Authenticate;
using IME.SpotDataApi.Services.Dashboard;
using IME.SpotDataApi.Services.Data;
using IME.SpotDataApi.Services.MainGroupLevel;
using IME.SpotDataApi.Services.Markets;
using IME.SpotDataApi.Services.RemoteData;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy  =>
                      {
                          if (allowedOrigins != null && allowedOrigins.Length > 0)
                          {
                              policy.WithOrigins(allowedOrigins)
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                          }
                      });
});

// Add services to the container.

builder.Services.AddHostedService<DataSyncService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMarketsService, MarketsService>();
builder.Services.AddScoped<IMainGroupService, MainGroupService>();
builder.Services.AddScoped<IGroupService, GroupService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<AppDataContext>(options =>
    options.UseSqlite(connectionString),
    ServiceLifetime.Singleton
);

builder.Services.Configure<SsoSettings>(
    builder.Configuration.GetSection(SsoSettings.SectionName)
);
builder.Services.Configure<ApiEndpoints>(
    builder.Configuration.GetSection(ApiEndpoints.SectionName)
);

builder.Services.AddHttpClient();

builder.Services.AddScoped<IDateHelper, DateHelper>();
builder.Services.AddScoped<ITokenManager, TokenManager>();


builder.Services.AddScoped(typeof(IRemoreOperationalResurceService<NewsNotification>), typeof(NotificationService<NewsNotification>));
builder.Services.AddScoped(typeof(IRemoreOperationalResurceService<SpotNotification>), typeof(NotificationService<SpotNotification>));
builder.Services.AddScoped(typeof(IRemoreOperationalResurceService<>), typeof(OperationalResurceService<>));
builder.Services.AddScoped(typeof(IRemotePublicResurceService<>), typeof(PublicResurceService<>));

builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<ITradeReportRepository, TradeReportRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddScoped(typeof(IDataRepository<>), typeof(DataRepository<>));
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();
