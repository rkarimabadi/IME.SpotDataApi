using IME.SpotDataApi.Data;
using IME.SpotDataApi.Helpers;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Authenticate;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Models.Notification;
using IME.SpotDataApi.Repository;
using IME.SpotDataApi.Services.Authenticate;
using IME.SpotDataApi.Services.Data;
using IME.SpotDataApi.Services.RemoteData;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHostedService<DataSyncService>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
