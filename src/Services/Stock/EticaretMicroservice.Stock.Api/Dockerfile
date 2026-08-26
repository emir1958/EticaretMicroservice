FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Stock/EticaretMicroservice.Stock.Api/EticaretMicroservice.Stock.Api.csproj", "src/Services/Stock/EticaretMicroservice.Stock.Api/"]
COPY ["src/Shared/EticaretMicroservice.Shared/EticaretMicroservice.Shared.csproj", "src/Shared/EticaretMicroservice.Shared/"]

RUN dotnet restore "src/Services/Stock/EticaretMicroservice.Stock.Api/EticaretMicroservice.Stock.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/Stock/EticaretMicroservice.Stock.Api"
RUN dotnet build "EticaretMicroservice.Stock.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Stock.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EticaretMicroservice.Stock.Api.dll"]