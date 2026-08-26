FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Payment/EticaretMicroservice.Payment.Api/EticaretMicroservice.Payment.Api.csproj", "src/Services/Payment/EticaretMicroservice.Payment.Api/"]
COPY ["src/Shared/EticaretMicroservice.Shared/EticaretMicroservice.Shared.csproj", "src/Shared/EticaretMicroservice.Shared/"]

RUN dotnet restore "src/Services/Payment/EticaretMicroservice.Payment.Api/EticaretMicroservice.Payment.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/Payment/EticaretMicroservice.Payment.Api"
RUN dotnet build "EticaretMicroservice.Payment.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Payment.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EticaretMicroservice.Payment.Api.dll"]