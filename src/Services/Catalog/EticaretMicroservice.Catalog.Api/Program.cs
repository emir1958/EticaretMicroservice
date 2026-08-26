using EticaretMicroservice.Catalog.Api.Services;
using EticaretMicroservice.Catalog.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EticaretMicroservice.Shared.Extensions; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSharedSwagger();

// --- 2. CORS POLİTİKASI ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddSingleton<IDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);

// Servis kayıtları
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- 4. MIDDLEWARE SIRALAMASI (ÇOK KRİTİK!) ---
app.UseCors("AllowAll");

// ⚠️ UseAuthentication mutlaka UseAuthorization'dan ÖNCE gelmelidir!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();