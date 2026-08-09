var builder = WebApplication.CreateBuilder(args);

// 1. YARP ve CORS Servislerini Ekle
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 2. Middleware sýralamasý
app.UseCors("AllowAll");

// Gelen istekleri appsettings.json'daki kurallara göre arkadaki servislere pasla
app.MapReverseProxy();

app.Run();