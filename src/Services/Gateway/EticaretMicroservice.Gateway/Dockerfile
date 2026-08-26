FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Gateway/EticaretMicroservice.Gateway/EticaretMicroservice.Gateway.csproj", "src/Services/Gateway/EticaretMicroservice.Gateway/"]

RUN dotnet restore "src/Services/Gateway/EticaretMicroservice.Gateway/EticaretMicroservice.Gateway.csproj"

COPY . .
WORKDIR "/src/src/Services/Gateway/EticaretMicroservice.Gateway"
RUN dotnet build "EticaretMicroservice.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EticaretMicroservice.Gateway.dll"]