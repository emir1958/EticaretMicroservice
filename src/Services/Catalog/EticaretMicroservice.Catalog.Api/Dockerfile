FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Catalog/EticaretMicroservice.Catalog.Api/EticaretMicroservice.Catalog.Api.csproj", "src/Services/Catalog/EticaretMicroservice.Catalog.Api/"]
COPY ["src/Shared/EticaretMicroservice.Shared/EticaretMicroservice.Shared.csproj", "src/Shared/EticaretMicroservice.Shared/"]

RUN dotnet restore "src/Services/Catalog/EticaretMicroservice.Catalog.Api/EticaretMicroservice.Catalog.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/Catalog/EticaretMicroservice.Catalog.Api"
RUN dotnet build "EticaretMicroservice.Catalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Catalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EticaretMicroservice.Catalog.Api.dll"]