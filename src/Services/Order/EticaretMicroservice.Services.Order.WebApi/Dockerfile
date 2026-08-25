# 1. RUNTIME AŞAMASI
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. BUILD AŞAMASI
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Proje dosyalarını (csproj) kopyalama (src/ öneki eklendi)
COPY ["src/Services/Order/EticaretMicroservice.Services.Order.WebApi/EticaretMicroservice.Services.Order.WebApi.csproj", "src/Services/Order/EticaretMicroservice.Services.Order.WebApi/"]
COPY ["src/Services/Order/EticaretMicroservice.Services.Order.Application/EticaretMicroservice.Services.Order.Application.csproj", "src/Services/Order/EticaretMicroservice.Services.Order.Application/"]
COPY ["src/Services/Order/EticaretMicroservice.Services.Order.Domain/EticaretMicroservice.Services.Order.Domain.csproj", "src/Services/Order/EticaretMicroservice.Services.Order.Domain/"]
COPY ["src/Services/Order/EticaretMicroservice.Services.Order.Infrastructure/EticaretMicroservice.Services.Order.Infrastructure.csproj", "src/Services/Order/EticaretMicroservice.Services.Order.Infrastructure/"]
COPY ["src/Shared/EticaretMicroservice.Shared/EticaretMicroservice.Shared.csproj", "src/Shared/EticaretMicroservice.Shared/"]

# Restore işlemi
RUN dotnet restore "src/Services/Order/EticaretMicroservice.Services.Order.WebApi/EticaretMicroservice.Services.Order.WebApi.csproj"

# Tüm kaynak kodları kopyala ve derle
COPY . .
WORKDIR "/src/src/Services/Order/EticaretMicroservice.Services.Order.WebApi"
RUN dotnet build "EticaretMicroservice.Services.Order.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. PUBLISH AŞAMASI
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Services.Order.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. FINAL AŞAMASI
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EticaretMicroservice.Services.Order.WebApi.dll"]